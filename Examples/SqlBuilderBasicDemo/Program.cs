using Examples.Models;
using Examples.Models.Filters;
using GenericFilters.SqlQueryBuilder.Dialects;
using Microsoft.EntityFrameworkCore;

/*
 * This demo provides same result as BasicDemo but using SqlQueryBuilder
 */

// Define a filter
var filter = new ProductsFilter
{
    ProductCategories = new() {"Electronics", "Home & Kitchen"},
    Description = "favorite",
    PriceTo = 200,
    StartDate = new DateTime(2025, 01, 01),
    PageSize = 5
};

using (var context = new ProductsDbContext())
{
    context.Database.OpenConnection();
    context.Database.EnsureCreated();

    context.SeedProducts();

    var sqlQuery = filter.BuildSqlQuery(new SqliteDialect());
    Console.WriteLine($"SQL Query: {sqlQuery}");

    var filteredProducts = context.Products.FromSqlRaw(sqlQuery);
    foreach (var product in filteredProducts.OrderBy(i => i.UnitPrice))
    {
        Console.WriteLine(product.Name);
    }
}
