---

# 🔍 GenericFilters [![NuGet](https://img.shields.io/nuget/v/genericFilters.svg)](https://www.nuget.org/packages/genericFilters/)

A powerful and extensible filtering framework for C# applications. `GenericFilters` enables dynamic, attribute-driven filtering logic for LINQ queries, in-memory collections, and Cosmos DB SDK queries.

---

## 🔧 Installation

### .NET CLI
```bash
dotnet add package GenericFilters
```

### Package Manager

Install-Package GenericFilters

---

## 📦 Features

- ✅ Attribute-based filtering with `FilterMemberAttribute`
- 🔄 Supports string, list, `DateTime` and numeric comparisons
- 🧠 Logical operations (`AND` / `OR`) between filters
- 🧰 Customizable filter behavior with `FilterOptions`
- 🧪 Built-in validation and error handling
- ⚙️ Expression tree generation for LINQ queries
- 🛢️ Raw SQL generation from a `Filter` via `BuildSqlQuery()`, with dialect, join and `EXISTS`-subquery support

---

## 📁 Components

### 1. `Filter`

An abstract base class that:
- Validates filter properties at runtime
- Generates LINQ expressions dynamically
- Supports pagination (`StartingIndex`, `PageSize`)
- Provides utility methods: `Any()`, `All()`, `GetQueryExpression()`

### 2. `FilterMemberAttribute`

Decorates filter properties to define:
- Target model property (`Name`)
- String comparison method (`Equals`, `Contains`)
- Case sensitivity
- Dates and numbers comparison operations
- Logical grouping (`And`, `Or`)
- Inclusion/exclusion in query generation

### 3. `FilterOptions`

Controls runtime behavior:
- `Optimistic`: If `true`, ignores missing model properties instead of throwing exceptions

---

## 🚀 Getting Started

### 1. Define a Model

```csharp
public record Product(string Name, List<string> Tags, 
    double UnitPrice, DateTime? CreatedAt);
```

### 2. Define a Filter

```csharp
using GenericFilters;

public class ProductFilter : Filter<Product>
{
    [FilterMember]
    public string Name { get; init; }

    [FilterMember(stringComparisonMethod: StringComparisonMethod.Contains, stringComparisonIgnoreCase: true)]
    public List<string> Tags { get; init; }
    
    [FilterMember("UnitPrice", comparisonOperation: ComparisonOperation.GreaterThanOrEqual)]
    public double? PriceFrom { get; init; }
    
    [FilterMember("UnitPrice", comparisonOperation: ComparisonOperation.LessThan)]
    public double? PriceTo { get; init; }

    [FilterMember("CreatedAt", comparisonOperation: ComparisonOperation.GreaterThanOrEqual)]
    public DateTime? StartDate { get; init; }

    [FilterMember("CreatedAt", comparisonOperation: ComparisonOperation.LessThanOrEqual)]
    public DateTime? EndDate { get; init; }
}
```

### 3. Apply the Filter

```csharp
using GenericFilters.Extensions;

var products = new List<Product>
{
    new Product("book", [ "education", "programming", "software" ], 
        58.90, new DateTime(2025, 2, 12)),
    new Product("phone", [ "android", "electronics" ],
        770.00, new DateTime(2023, 6, 8)),
    new Product("laptop", [ "linux", "electronics", "entertaiment" ], 
        3999.99, new DateTime(2024, 7, 22)),
};

var filter = new ProductFilter
{
    Name = "book", 
    Tags = [ "education", "engineering" ], 
    PriceFrom = 40.00,
    PriceTo = 100.00,
    StartDate = new DateTime(2025, 2, 1), 
    EndDate = new DateTime(2025, 3, 1)
};

var filteredProducts = products.FilterBy(filter).ToList();
```

### 3.1. Same scenario using GetQueryExpression() method

```csharp
var expression = filter.GetQueryExpression();

var filteredProducts = products.AsQueryable()
    .Where(expression)
    .ToList();
```

▶️ [Run this code on .NET Fiddle][dotnet1]

[dotnet1]: https://dotnetfiddle.net/id3pVo

---

## ⚙️ Advanced Options

### Optimistic Filtering

```csharp
var options = new FilterOptions { Optimistic = true };
var expression = filter.GetQueryExpression(options);
```

This allows filters to skip missing model properties without throwing exceptions.

### IgnoreInQueryExpression

When IgnoreInQueryExpression is set to `true` in the FilterMember attribute, that property is skipped when we build 
Filter expression   

```csharp
[FilterMember(ignoreInQueryExpression: true)]
public List<string> Items { get; init; }
```
You can use that parameter in cases when you need to implement any custom filtering logic.
More details about custom filtering is provided under section related to GetQueryExpressionExt method

It is the same when we don't provide any attribute for that property at all, but the difference is,
If we provide FilterMember, that property will be taken into consideration when we call 
getHashCode(), Any() or All() methods of the Filter class. 

### Nested properties

Starting from ver. 1.1.0 GenericFilters supports nested properties using dot '.' notation.

### 1. Define a Model

```csharp
public record Category(int Id, string Name);

public class Product
{
    public string Name { get; init; }
	public Category Category { get; init; }
    public List<string> Tags { get; init; }
    public double UnitPrice { get; init; } 
    public DateTime CreatedAt { get; init; }
};
```

### 2. Define a Filter

```csharp
using GenericFilters;

public class ProductFilter : Filter<Product>
{
    [FilterMember]
    public string Name { get; init; }

    [FilterMember("Category.Name")]
    public string Category { get; init; }

	[FilterMember(stringComparisonMethod: StringComparisonMethod.Contains, stringComparisonIgnoreCase: true)]
    public List<string> Tags { get; init; }
    
    [FilterMember("UnitPrice", comparisonOperation: ComparisonOperation.GreaterThanOrEqual)]
    public double? PriceFrom { get; init; }
    
    [FilterMember("UnitPrice", comparisonOperation: ComparisonOperation.LessThan)]
    public double? PriceTo { get; init; }

    [FilterMember("CreatedAt", comparisonOperation: ComparisonOperation.GreaterThanOrEqual)]
    public DateTime? StartDate { get; init; }

    [FilterMember("CreatedAt", comparisonOperation: ComparisonOperation.LessThanOrEqual)]
    public DateTime? EndDate { get; init; }
}
```

### 3. Apply the Filter

```csharp
using GenericFilters.Extensions;

var category = new Category(1, "storage");
		
var products = new List<Product>
{
    new Product
    {
        Name = "External Storage Bundle",
        Category = category,
        Tags = new List<string> { "storage", "bundle", "external" },
        UnitPrice = 129.99,
        CreatedAt = new DateTime(2025, 2, 1)
    },
    new Product
    {
        Name = "Portable Backup Kit",
        Category = null,
        Tags = new List<string> { "backup", "portable", "data", "storage" },
        UnitPrice = 89.99,
        CreatedAt = new DateTime(2025, 2, 1)
    },
    new Product
    {
        Name = "Hard Drive",
        Category = category,
        Tags = new List<string> { "storage", "hard drive" },
        UnitPrice = 59.99,
        CreatedAt = new DateTime(2025, 2, 1)
    }
};

var filter = new ProductFilter
{
    Category = "storage",
    Tags = [ "data", "storage" ], 
    PriceFrom = 80.00,
    PriceTo = 150.00,
    StartDate = new DateTime(2025, 1, 1), 
};

var filteredProducts = products.AsQueryable()
    .FilterBy(filter)
    .ToList();
```

▶️ [Run this code on .NET Fiddle][dotnet2]

[dotnet2]: https://dotnetfiddle.net/Kf6WFD

### GetQueryExpressionExt method

In some cases in our model or our filter we may have some complex properties not handled by the Filter
out of the box.  
Or probably we need to provide some specific logic with extra conditions.

In that case we can apply the following approach:
- Apply FilterMember attributes for all strings, numeric etc. properties as usually.
- Mark all properties with custom logic we are going to provide as IgnoreInQueryExpression
- Override GetQueryExpressionExt and add all custom logic using LinqKit or build Linq Expression in any another way.

When we call either GetQueryExpression or FilterBy, that custom Linq Expression will be added to the end of
Expression generated for our 'standard' filter behind the scene.

Here is an example of using that approach to filter by ProductItem type property:

### 1. Define a Model

```csharp
public class Product
{
    public string Name { get; init; }
    public List<string> Tags { get; init; }
    public double UnitPrice { get; init; } 
    public List<ProductItem> Items { get; init; }
    public DateTime CreatedAt { get; init; }
};

public record ProductItem (string Sku, int Quantity);
```

### 2. Define a Filter

```csharp
using System.Linq.Expressions;
using GenericFilters;
using LinqKit;

public class ProductFilter : Filter<Product>
{
    [FilterMember]
    public string Name { get; init; }

    [FilterMember(stringComparisonMethod: StringComparisonMethod.Contains, stringComparisonIgnoreCase: true)]
    public List<string> Tags { get; init; }
    
    [FilterMember(ignoreInQueryExpression: true)]
    public List<string> Items { get; init; }
    
    [FilterMember("UnitPrice", comparisonOperation: ComparisonOperation.GreaterThanOrEqual)]
    public double? PriceFrom { get; init; }
    
    [FilterMember("UnitPrice", comparisonOperation: ComparisonOperation.LessThan)]
    public double? PriceTo { get; init; }

    [FilterMember("CreatedAt", comparisonOperation: ComparisonOperation.GreaterThanOrEqual)]
    public DateTime? StartDate { get; init; }

    [FilterMember("CreatedAt", comparisonOperation: ComparisonOperation.LessThanOrEqual)]
    public DateTime? EndDate { get; init; }
    
    protected override Expression<Func<Product, bool>> GetQueryExpressionExt(FilterOptions filterOptions)
    {
        var predicate = PredicateBuilder.New<Product>();
        
        // Build custom behaviour for Items using LinqKit
        predicate.And(i => Items
            .Any(t => i.Items.Any(i => i.Sku == t && i.Quantity > 0)));
        
        return predicate;
    }
}
```

### 3. Apply the Filter

```csharp
using GenericFilters.Extensions;

var products = new List<Product>
{
    new Product
    {
        Name = "External Storage Bundle",
        Tags = new List<string> { "storage", "bundle", "external" },
        UnitPrice = 129.99,
        CreatedAt = new DateTime(2025, 2, 1),
        Items = new List<ProductItem>
        {
            new ProductItem("HD-1001", 25),
            new ProductItem("USB-64GB", 100),
            new ProductItem("SD-128GB", 50)
        }
    },
    new Product
    {
        Name = "Portable Backup Kit",
        Tags = new List<string> { "backup", "portable", "data", "storage" },
        UnitPrice = 89.99,
        CreatedAt = new DateTime(2025, 2, 1),
        Items = new List<ProductItem>
        {
            new ProductItem("HD-1001", 20),
            new ProductItem("CASE-01", 30)
        }
    },
    new Product
    {
        Name = "Hard Drive",
        Tags = new List<string> { "storage", "hard drive" },
        UnitPrice = 59.99,
        CreatedAt = new DateTime(2025, 2, 1),
        Items = new List<ProductItem>
        {
            new ProductItem("HD-2002", 40)
        }
    }
};

var filter = new ProductFilter
{
    Tags = [ "data", "storage" ], 
    Items = ["HD-1001" ],
    PriceFrom = 80.00,
    PriceTo = 150.00,
    StartDate = new DateTime(2025, 1, 1), 
};

var filteredProducts = products.AsQueryable()
    .FilterBy(filter)
    .ToList();
```

▶️ [Run this code on .NET Fiddle][dotnet3]

[dotnet3]: https://dotnetfiddle.net/IAPBu2

### LogicalOperation
`FilterMember` attribute supports LogicalOperation parameter. 
By default, all properties are selected using `And` logic. 
There is also `Or` option available. But we should be careful in case of mixing `And` and `Or` together.
Since there is no grouping option available, it depends on attributes order, 
so we need to take that order and logical operations priority into account.
Otherwise, we may end-up with non-deterministic result.

```csharp
public record TestModel(string Prop1, string Prop2, string Prop3, string Prop4);

public class TestFilter : Filter<TestModel>
{
    [FilterMember]
    public string Prop1 { get; init; }
    
    [FilterMember(logicalOperation: LogicalOperation.Or)]
    public string Prop2 { get; init; }
    
    [FilterMember]
    public string Prop3 { get; init; }
    
    [FilterMember(logicalOperation: LogicalOperation.Or)]
    public string Prop4 { get; init; }
}

var model = new List<TestModel> { new TestModel("a", "b", "c", "d") };
var filter = new TestFilter
{
    Prop1 = "a",
    Prop2 = "b",
    Prop3 = "c",
    Prop4 = "z",
};

var query = filter.GetQueryExpression();
var result = model.AsQueryable().Where(query).ToList();

Console.WriteLine(query.ToString());
Console.WriteLine(result.Count);
```

Result:<br>
<i>
Param_0 => (((Param_0.Prop1.Equals("a") OrElse Param_0.Prop2.Equals("b")) AndAlso Param_0.Prop3.Equals("c")) OrElse Param_0.Prop4.Equals("z"))
<br>
1
</i>

▶️ [Run this code on .NET Fiddle][dotnet4]

[dotnet4]: https://dotnetfiddle.net/p0iFmB

In order to avoid such sort of issues, 
it is recommended approach to provide our custom logic in the GetQueryExpressionExt method.

### Pagination support

There are 2 properties provided in the Filter class in order to support pagination 

```csharp
public int StartIndex { get; set; } = -1;
public int PageSize { get; set; } = -1;
```
Those properties can be set in the Filter and tracked e.g. on UI side. 
When we call FilterBy, internally it will be added Skip() and Take() to our expression. 
If we are using GetQueryExpression, we need to build that logic by ourselves

```csharp
var filteredProducts = products.AsQueryable()
    .Where(filter.GetQueryExpression())
    .Take(filter.StartIndex).Skip(filter.PageSize);
```

---

## 🧪 Validation & Safety

- Only supports `string`, `List<string>`, `DateTime`, `double`, `decimal` and `int` filter types, with nullables
- Throws `FilterException` for unsupported types or misconfigurations
- Ensures at least one valid filter is defined

---

## 📌 Notes

- Not all features are compatible with `IQueryable` in Entity Framework
- Case-insensitive filtering may impact performance if DB collation is not case-sensitive
- You can override `GetQueryExpressionExt()` for custom logic

---

## 🛢️ SQL Query Builder

Besides generating LINQ expressions, a `Filter` can produce a ready-to-run SQL `SELECT` statement directly, via `BuildSqlQuery()`. This is useful when you want to run the query as raw SQL (e.g. EF Core's `FromSqlRaw`/`Database.SqlQuery`, Dapper, plain ADO.NET) instead of going through `IQueryable`.

### 1. Define a Model

```csharp
using System.ComponentModel.DataAnnotations;

public class Category
{
    [Key]
    public int CategoryId { get; init; }
    public string Name { get; init; }
}

public class Product
{
    [Key]
    public int ProductId { get; init; }
    public int CategoryId { get; init; }
    public Category Category { get; init; }
    public string Name { get; init; }
    public string Description { get; init; }
    public double UnitPrice { get; init; }
    public DateTime DateAdded { get; init; }
}
```

### 2. Define a Filter

```csharp
using GenericFilters;

public class ProductsFilter : Filter<Product>
{
    [FilterMember("Category.Name")]
    public List<string> ProductCategories { get; init; }

    [FilterMember(stringComparisonMethod: StringComparisonMethod.Contains, stringComparisonIgnoreCase: true)]
    public string Description { get; init; }

    [FilterMember("UnitPrice", comparisonOperation: ComparisonOperation.LessThan)]
    public double? PriceTo { get; init; }

    [FilterMember("DateAdded", comparisonOperation: ComparisonOperation.GreaterThanOrEqual)]
    public DateTime? StartDate { get; init; }
}
```

### 3. Build the SQL query

```csharp
using GenericFilters.SqlQueryBuilder.Dialects;

var filter = new ProductsFilter
{
    ProductCategories = new() { "Electronics", "Home & Kitchen" },
    Description = "favorite",
    PriceTo = 200,
    StartDate = new DateTime(2025, 01, 01)
};

var sqlQuery = filter.BuildSqlQuery(new SqliteDialect());
```

Generated SQL:

```sql
SELECT "p"."ProductId", "p"."CategoryId", "p"."DateAdded", "p"."Description", "p"."Name", "p"."UnitPrice"
FROM "Products" AS "p"
INNER JOIN "Categories" AS "c" ON "p"."CategoryId" = "c"."CategoryId"
WHERE "c"."Name" IN ('Electronics', 'Home & Kitchen') AND instr(lower("p"."Description"), 'favorite') > 0 AND "p"."UnitPrice" < 200.0 AND "p"."DateAdded" >= '2025-01-01 00:00:00'
```

You can then run it directly, e.g. with EF Core:

```csharp
var filteredProducts = context.Products.FromSqlRaw(sqlQuery);
```

▶️ Full runnable example: [SqlBuilderBasicDemo](Examples/SqlBuilderBasicDemo/Program.cs)

### Dialects

`BuildSqlQuery` takes an `ISqlDialect`, which controls identifier quoting, literal formatting, table/column naming, and how `Contains`/`StartsWith`/`EndsWith` string predicates are rendered. `AnsiSqlDialect` is the default; `SqliteDialect` is provided out of the box and can be used as a template for other databases (Postgres, SQL Server, etc.) by overriding just the members that differ:

```csharp
public class SqliteDialect : AnsiSqlDialect
{
    public override string BuildContainsPredicate(string columnSql, string value, bool ignoreCase) =>
        ignoreCase
            ? $"instr(lower({columnSql}), '{EscapeString(value.ToLowerInvariant())}') > 0"
            : $"instr({columnSql}, '{EscapeString(value)}') > 0";
}
```

### Joins

Filters on nested properties (e.g. `[FilterMember("Category.Name")]`) are automatically translated into an `INNER JOIN`, resolved by convention from the navigation property's `[Key]` and a matching `{Nav}Id` / `{Nav}{Key}` foreign key on the model - the same convention EF Core itself uses.

### Filtering by a collection navigation (Any / EXISTS)

Custom logic added via [`GetQueryExpressionExt`](#getqueryexpressionext-method) that calls `.Any()` on a `List<T>` navigation property is translated into a correlated `EXISTS` subquery:

```csharp
protected override Expression<Func<Product, bool>> GetQueryExpressionExt(FilterOptions filterOptions)
{
    var predicate = PredicateBuilder.New<Product>();
    var tags = Tags.ConvertAll(i => i.ToLower());
    predicate.And(p => tags.Any(t => p.Tags.Any(i => i.Name.Equals(t))));
    return predicate;
}
```

generates:

```sql
... AND (EXISTS (SELECT 1 FROM "Tags" AS "t" WHERE "t"."ProductId" = "p"."ProductId" AND "t"."Name" = LOWER('new'))
      OR EXISTS (SELECT 1 FROM "Tags" AS "t2" WHERE "t2"."ProductId" = "p"."ProductId" AND "t2"."Name" = LOWER('trending')))
```

▶️ Full runnable example: [SqlBuilderAdvancedDemo](Examples/SqlBuilderAdvancedDemo/Program.cs)

### Limitations

- Only `INNER JOIN` for to-one navigations, resolved by naming convention (no explicit mapping configuration)
- No `Skip`/`Take` support yet - `StartIndex`/`PageSize` are ignored by `BuildSqlQuery`
- Intended as a lightweight, best-effort SQL generator, not a full query-translation engine like EF Core's

---
