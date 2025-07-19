using Examples.Models;
using Examples.Models.Filters;
using GenericFilters.Extensions;
using Microsoft.EntityFrameworkCore;

// Define a filter
var filter = new ProductsFilter
{
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

    var filteredProducts = context.Products
        .OrderBy(i => i.UnitPrice)
        .FilterBy(filter);
        
    foreach (var product in filteredProducts)
    {
        Console.WriteLine(product.Name);
    }
}
