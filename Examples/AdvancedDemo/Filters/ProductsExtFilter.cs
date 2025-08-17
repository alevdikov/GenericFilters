using System.Linq.Expressions;
using Examples.Models.Filters;
using Examples.Models.Models;
using GenericFilters;
using LinqKit;

namespace AdvancedDemo.Filters;

public class ProductsExtFilter : ProductsFilter
{
    protected override Expression<Func<Product, bool>> GetQueryExpressionExt(FilterOptions filterOptions)
    {
        var predicate = PredicateBuilder.New<Product>();
        
        // Provide custom behaviour for Tags using LinqKit
        // Note: We can achieve the same result using nested property name
        // in the FilterMember attribute
        var tags = Tags.ConvertAll(i => i.ToLower());
        predicate.And(p => tags
            .Any(t => p.Tags.Any(i => i.Name.Equals(t.ToLower()))));
        
        return predicate;
    }
}
