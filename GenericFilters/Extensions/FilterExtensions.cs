using System.Collections.Generic;
using System.Linq;

namespace GenericFilters.Extensions;

public static class FilterExtensions
{
    public static IQueryable<TModel> FilterBy<TModel>(this IQueryable<TModel> source, Filter<TModel> filter) 
        where TModel : class
    {
        var query = source.Where(filter.GetQueryExpression());
        if(filter.StartIndex != -1)
            query = query.Skip(filter.StartIndex).Take(filter.PageSize);
        if(filter.PageSize != -1)
            query = query.Take(filter.PageSize);
        return query;
    }
    
    public static IEnumerable<TModel> FilterBy<TModel>(this IEnumerable<TModel> source, Filter<TModel> filter) 
        where TModel : class
    {
        var query = source.Where(filter.GetQueryExpression().Compile());
        if(filter.StartIndex != -1)
            query = query.Skip(filter.StartIndex).Take(filter.PageSize);
        if(filter.PageSize != -1)
            query = query.Take(filter.PageSize);
        return query;
    }
}
