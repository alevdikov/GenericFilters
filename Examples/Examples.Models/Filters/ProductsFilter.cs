using Examples.Models.Models;
using GenericFilters;

namespace Examples.Models.Filters;

public class ProductsFilter : Filter<Product>
{
    [FilterMember]
    public string Name { get; init; }

    [FilterMember(ignoreInQueryExpression: true)]
    public List<string> ProductCategories { get; init; }

    [FilterMember(stringComparisonMethod: StringComparisonMethod.Contains, stringComparisonIgnoreCase: true)]
    public string Description { get; init; }
    
    [FilterMember(ignoreInQueryExpression: true)]
    public List<string> Tags { get; init; }
 
    [FilterMember(ignoreInQueryExpression: true)]
    public int StockQuantity { get; init; }
    
    [FilterMember("UnitPrice", comparisonOperation: ComparisonOperation.GreaterThanOrEqual)]
    public double? PriceFrom { get; init; }
    
    [FilterMember("UnitPrice", comparisonOperation: ComparisonOperation.LessThan)]
    public double? PriceTo { get; init; }

    [FilterMember("DateAdded", comparisonOperation: ComparisonOperation.GreaterThanOrEqual)]
    public DateTime? StartDate { get; init; }

    [FilterMember("DateAdded", comparisonOperation: ComparisonOperation.LessThanOrEqual)]
    public DateTime? EndDate { get; init; }
}
