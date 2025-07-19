using System.Linq.Expressions;
using Examples.Models.Filters;
using Examples.Models.Models;
using GenericFilters;
using LinqKit;

namespace AdvancedDemo.Models;

public class ProductsExtFilter : ProductsFilter
{
    protected override Expression<Func<Product, bool>> GetQueryExpressionExt(FilterOptions filterOptions)
    {
        var predicate = PredicateBuilder.New<Product>();
        
        // Build custom behaviour for ProductCategories and Tags using LinqKit
        var categories = ProductCategories.ConvertAll(i => i.ToLower());
        predicate.And(i => categories.Contains(i.Category.Name.ToLower()));
        
        var tags = Tags.ConvertAll(i => i.ToLower());
        predicate.And(i => tags
            .Any(t => i.Tags.Any(i => i.Name.Equals(t.ToLower()))));
        
        return predicate;
    }
}