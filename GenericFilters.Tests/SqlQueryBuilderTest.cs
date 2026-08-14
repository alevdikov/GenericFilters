using System.ComponentModel.DataAnnotations;
using System.Linq.Expressions;
using GenericFilters.SqlQueryBuilder;
using GenericFilters.SqlQueryBuilder.Dialects;
using Xunit;

namespace GenericFilters.Tests;

public class SqlQueryBuilderTest
{
    #region SELECT / FROM / JOIN

    [Fact]
    public void BuildQuery_SelectAndFrom_ColumnOrder_Test()
    {
        Expression<Func<Invoice, bool>> filter = x => x.Name == "Test";

        var sql = new SqlQueryBuilder<Invoice>().BuildQuery(filter);

        const string expectedSelectAndFrom =
            "SELECT \"i\".\"Id\", \"i\".\"CategoryId\", \"i\".\"CreatedDate\", \"i\".\"Description\", " +
            "\"i\".\"Name\", \"i\".\"Price\", \"i\".\"Quantity\", \"i\".\"UpdatedDate\"\n" +
            "FROM \"Invoices\" AS \"i\"";

        Assert.StartsWith(expectedSelectAndFrom, sql);
    }

    [Fact]
    public void BuildQuery_Join_Test()
    {
        Expression<Func<Invoice, bool>> filter = x => x.Category.Name == "Electronics";

        var sql = new SqlQueryBuilder<Invoice>().BuildQuery(filter);

        Assert.Contains(
            "INNER JOIN \"Categories\" AS \"c\" ON \"i\".\"CategoryId\" = \"c\".\"CategoryId\"", sql);
        Assert.Contains("WHERE \"c\".\"Name\" = 'Electronics'", sql);
    }

    [Fact]
    public void BuildQuery_RedundantJoinedNavNullCheck_Dropped_Test()
    {
        Expression<Func<Invoice, bool>> filter = x => x.Category != null && x.Category.Name == "Electronics";

        var sql = new SqlQueryBuilder<Invoice>().BuildQuery(filter);

        Assert.DoesNotContain("IS NOT NULL", sql);
        Assert.Contains("WHERE \"c\".\"Name\" = 'Electronics'", sql);
    }

    #endregion

    #region WHERE clause operators

    [Fact]
    public void BuildQuery_Equality_Test()
    {
        Expression<Func<Invoice, bool>> filter = x => x.Name == "Test1";

        var sql = new SqlQueryBuilder<Invoice>().BuildQuery(filter);

        Assert.Contains("WHERE \"i\".\"Name\" = 'Test1'", sql);
    }

    [Fact]
    public void BuildQuery_NullEquality_Test()
    {
        Expression<Func<Invoice, bool>> isNull = x => x.UpdatedDate == null;
        Expression<Func<Invoice, bool>> isNotNull = x => x.UpdatedDate != null;

        var builder = new SqlQueryBuilder<Invoice>();

        Assert.Contains("\"i\".\"UpdatedDate\" IS NULL", builder.BuildQuery(isNull));
        Assert.Contains("\"i\".\"UpdatedDate\" IS NOT NULL", builder.BuildQuery(isNotNull));
    }

    [Fact]
    public void BuildQuery_AndLogic_NoParens_Test()
    {
        Expression<Func<Invoice, bool>> filter = x => x.Price > 10.0 && x.Price <= 200.0;

        var sql = new SqlQueryBuilder<Invoice>().BuildQuery(filter);

        Assert.Contains("WHERE \"i\".\"Price\" > 10.0 AND \"i\".\"Price\" <= 200.0", sql);
    }

    [Fact]
    public void BuildQuery_OrLogic_Parenthesized_Test()
    {
        Expression<Func<Invoice, bool>> filter = x => x.CategoryId == 1 || x.CategoryId == 2;

        var sql = new SqlQueryBuilder<Invoice>().BuildQuery(filter);

        Assert.Contains("WHERE (\"i\".\"CategoryId\" = 1 OR \"i\".\"CategoryId\" = 2)", sql);
    }

    [Fact]
    public void BuildQuery_StartsWith_EndsWith_Test()
    {
        Expression<Func<Invoice, bool>> startsWith = x => x.Name.StartsWith("Pro");
        Expression<Func<Invoice, bool>> endsWith = x => x.Name.EndsWith("duct");

        var builder = new SqlQueryBuilder<Invoice>();

        Assert.Contains("\"i\".\"Name\" LIKE 'Pro%'", builder.BuildQuery(startsWith));
        Assert.Contains("\"i\".\"Name\" LIKE '%duct'", builder.BuildQuery(endsWith));
    }

    [Fact]
    public void BuildQuery_ToLower_Test()
    {
        Expression<Func<Invoice, bool>> filter = x => x.Name.ToLower() == "test";

        var sql = new SqlQueryBuilder<Invoice>().BuildQuery(filter);

        Assert.Contains("LOWER(\"i\".\"Name\") = 'test'", sql);
    }

    [Fact]
    public void BuildQuery_StringContains_DefaultDialect_Test()
    {
        Expression<Func<Invoice, bool>> filter = x => x.Description.Contains("favorite");

        var sql = new SqlQueryBuilder<Invoice>().BuildQuery(filter);

        Assert.Contains("\"i\".\"Description\" LIKE '%favorite%'", sql);
    }

    [Fact]
    public void BuildQuery_ConstantListContains_WithJoin_Test()
    {
        // Built via Expression.Constant directly (as Filter's list-based FilterMembers do)
        // rather than a captured local, since captured locals compile to closure-field
        // member access rather than a ConstantExpression.
        var parameter = Expression.Parameter(typeof(Invoice), "x");
        var categoryName = Expression.Property(Expression.Property(parameter, nameof(Invoice.Category)), nameof(Category.Name));
        var categories = Expression.Constant(new List<string> { "Electronics", "Home & Kitchen" });
        var containsCall = Expression.Call(categories, typeof(List<string>).GetMethod("Contains", [ typeof(string) ]), categoryName);
        var filter = Expression.Lambda<Func<Invoice, bool>>(containsCall, parameter);

        var sql = new SqlQueryBuilder<Invoice>().BuildQuery(filter);

        Assert.Contains(
            "INNER JOIN \"Categories\" AS \"c\" ON \"i\".\"CategoryId\" = \"c\".\"CategoryId\"", sql);
        Assert.Contains("WHERE \"c\".\"Name\" IN ('Electronics', 'Home & Kitchen')", sql);
    }

    #endregion

    #region Any() -> EXISTS subquery / unrolled OR

    [Fact]
    public void BuildQuery_NavigationCollectionAny_ProducesExistsSubquery_Test()
    {
        Expression<Func<Product, bool>> filter = x => x.Tags.Any(t => t.Name == "new");

        var sql = new SqlQueryBuilder<Product>().BuildQuery(filter);

        Assert.Contains(
            "EXISTS (SELECT 1 FROM \"Tags\" AS \"t\" WHERE \"t\".\"ProductId\" = \"p\".\"ProductId\" AND \"t\".\"Name\" = 'new')",
            sql);
    }

    [Fact]
    public void BuildQuery_LocalCollectionAny_UnrolledIntoOr_Test()
    {
        var names = new List<string> { "Alpha", "Beta" };
        Expression<Func<Invoice, bool>> filter = x => names.Any(n => n == x.Name);

        var sql = new SqlQueryBuilder<Invoice>().BuildQuery(filter);

        Assert.Contains("('Alpha' = \"i\".\"Name\" OR 'Beta' = \"i\".\"Name\")", sql);
    }

    [Fact]
    public void BuildQuery_LocalCollectionAny_Empty_Test()
    {
        var names = new List<string>();
        Expression<Func<Invoice, bool>> filter = x => names.Any(n => n == x.Name);

        var sql = new SqlQueryBuilder<Invoice>().BuildQuery(filter);

        Assert.Contains("(1 = 0)", sql);
    }

    #endregion

    #region Dialects

    [Fact]
    public void BuildQuery_SqliteDialect_ContainsPredicate_Test()
    {
        Expression<Func<Invoice, bool>> filter = x => x.Description.ToLower().Contains("favorite");

        var sql = new SqlQueryBuilder<Invoice>().BuildQuery(filter, new SqliteDialect());

        Assert.Contains("instr(lower(\"i\".\"Description\"), 'favorite') > 0", sql);
    }

    [Fact]
    public void BuildQuery_NullDialect_DefaultsToAnsi_Test()
    {
        Expression<Func<Invoice, bool>> filter = x => x.Name == "Test";

        var withNull = new SqlQueryBuilder<Invoice>().BuildQuery(filter, null);
        var withExplicitAnsi = new SqlQueryBuilder<Invoice>().BuildQuery(filter, new AnsiSqlDialect());

        Assert.Equal(withExplicitAnsi, withNull);
    }

    #endregion

    #region Filter<TModel>.BuildSqlQuery() integration

    [Fact]
    public void BuildSqlQuery_FromAttributeDrivenFilter_Test()
    {
        var filter = new InvoiceFilter
        {
            CategoryNames = [ "Electronics", "Home & Kitchen" ],
            PriceTo = 200
        };

        var sql = filter.BuildSqlQuery(new SqliteDialect());

        Assert.Contains(
            "INNER JOIN \"Categories\" AS \"c\" ON \"i\".\"CategoryId\" = \"c\".\"CategoryId\"", sql);
        Assert.Contains("\"c\".\"Name\" IN ('Electronics', 'Home & Kitchen')", sql);
        Assert.Contains("\"i\".\"Price\" < 200.0", sql);
    }

    #endregion
}

#region Test models

public class Category
{
    [Key]
    public int CategoryId { get; init; }
    public string Name { get; init; }
}

public class Invoice
{
    [Key]
    public int Id { get; init; }
    public string Name { get; init; }
    public string Description { get; init; }
    public double Price { get; init; }
    public int Quantity { get; init; }
    public DateTime CreatedDate { get; init; }
    public DateTime? UpdatedDate { get; init; }
    public int CategoryId { get; init; }
    public Category Category { get; init; }
}

public class Product
{
    [Key]
    public int ProductId { get; init; }
    public string Name { get; init; }
    public List<Tag> Tags { get; init; }
}

public class Tag
{
    [Key]
    public int TagId { get; init; }
    public string Name { get; init; }
    public int ProductId { get; init; }
}

#endregion

#region Test filters

public class InvoiceFilter : Filter<Invoice>
{
    [FilterMember(name: "Category.Name")]
    public List<string> CategoryNames { get; set; }

    [FilterMember(name: "Price", comparisonOperation: ComparisonOperation.LessThan)]
    public double? PriceTo { get; set; }
}

#endregion