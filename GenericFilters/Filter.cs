using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using LinqKit;

namespace GenericFilters;
public abstract class Filter<TModel> where TModel : class
{
    public int StartIndex { get; set; } = -1;
    public int PageSize { get; set; } = -1;

    protected Filter()
    {
        var isValid = false;

        foreach (var property in GetType().GetProperties())
        {
            var hasFilterAttribute = Attribute.IsDefined(property, typeof(FilterMemberAttribute));
            if (hasFilterAttribute)
            {
                isValid = true;
                if (!IsAllowedType(property.PropertyType))
                    throw new FilterException($"{property.Name} - Filter member with type {property.PropertyType} is not supported.");
            }
        }

        if (!isValid)
            throw new FilterException("You need to specify at least one Filter element using FilterMember attribute");
    }

    public override int GetHashCode()
    {
        var hashCode = 0;

        foreach (var property in GetType().GetProperties())
        {
            var hasFilterAttribute = Attribute.IsDefined(property, typeof(FilterMemberAttribute));
            if (hasFilterAttribute)
            {
                if (property.PropertyType == typeof(List<string>))
                {
                    var list = property.GetValue(this) as List<string>;
                    if (list != null && list.Any())
                        hashCode ^= list.Select(i => i.GetHashCode()).Aggregate((t, n) => t ^ n);
                }
                else if (property.PropertyType == typeof(DateTime) || property.PropertyType == typeof(DateTime?))
                {
                    if (property.GetValue(this) != null)
                        hashCode ^= property.GetValue(this).GetHashCode();
                }
                else if (IsAllowedType(property.PropertyType))
                {
                    if (property.GetValue(this) != null)
                        hashCode ^= property.GetValue(this).ToString().GetHashCode();
                }
                else
                {
                    var error = $"Filter member with type {property.PropertyType} is not supported." +
                        " You need to provide custom implementation for GetHashCode() method";
                    throw new FilterException(error);
                }
            }
            else
            {
                // Include special properties StartingIndex and PageSize to hash calculation as well
                if (property.Name == "StartingIndex" || property.Name == "PageSize")
                {
                    hashCode ^= property.GetValue(this).ToString().GetHashCode();
                }
            }
        }

        return hashCode;
    }

    public virtual bool Any()
    {
        foreach (var property in GetType().GetProperties())
        {
            var hasFilterAttribute = Attribute.IsDefined(property, typeof(FilterMemberAttribute));
            if (hasFilterAttribute)
            {
                if (property.PropertyType == typeof(List<string>))
                {
                    var list = property.GetValue(this) as List<string>;
                    if (list != null && list.Any())
                        return true;
                }
                if (property.GetValue(this) != null) 
                    return true;
            }
        }

        return false;
    }

    public virtual bool All()
    {
        foreach (var property in GetType().GetProperties())
        {
            var hasFilterAttribute = Attribute.IsDefined(property, typeof(FilterMemberAttribute));
            if (hasFilterAttribute)
            {
                if (property.PropertyType == typeof(List<string>))
                {
                    var list = property.GetValue(this) as List<string>;
                    if (list is null || !list.Any())
                        return false;
                }
                if(property.GetValue(this) == null)
                    return false;
            }
        }

        return true;
    }

    protected virtual Expression<Func<TModel, bool>> GetQueryExpressionExt(FilterOptions filterOptions)
    {
        return null;
    }
    
    public Expression<Func<TModel, bool>> GetQueryExpression(FilterOptions filterOptions = null)
    {
        var predicate = PredicateBuilder.New(GetQueryExpressionInt(filterOptions));
        // Get custom expression if provide in overrides 
        var expression = GetQueryExpressionExt(filterOptions);
        if(expression != null)
            predicate.And(expression);

        return predicate;
    }
    
    private Expression<Func<TModel, bool>> GetQueryExpressionInt(FilterOptions filterOptions)
    {
        var predicate = PredicateBuilder.New<TModel>();
        foreach (var filterProperty in GetType().GetProperties())
        {
            if (filterProperty.GetCustomAttribute(typeof(FilterMemberAttribute)) is FilterMemberAttribute attribute)
            {
                if (attribute.IgnoreInQueryExpression) continue;

                var propertyName = attribute.Name ?? filterProperty.Name;
                
                var modelProperty = GetModelProperty(propertyName);
                if (modelProperty == null)
                {
                    if (filterOptions == null || !filterOptions.Optimistic)
                    { 
                        string error = $"Model doesn't contain any property with name {propertyName} required by Filter," + 
                                     " or corresponding property path doesn't exist." +             
                                     " You need to add that property, decorate it in the Filter class using FilterMember attribute," +
                                     " or provide custom implementation for GetQueryExpression() method";
                        throw new FilterException(error);
                    }
                    continue;
                }

                if (filterProperty.PropertyType == typeof(string))
                {
                    var strValue = filterProperty.GetValue(this) as string;
                    if (strValue != null && (strValue != "" || !attribute.IgnoreIfEmpty))
                    {
                        Expression<Func<TModel, bool>> expression;
                        if (attribute.StringComparisonMethod == StringComparisonMethod.Equals)
                            expression = FilterExpressions<TModel>.GetStringEqualsExpression(propertyName, strValue, attribute.StringComparisonIgnoreCase);
                        else if (attribute.StringComparisonMethod == StringComparisonMethod.Contains)
                            expression = FilterExpressions<TModel>.GetStringContainsExpression(propertyName, strValue, attribute.StringComparisonIgnoreCase);
                        else
                            throw new FilterException($"Illegal strings comparison method: {attribute.StringComparisonMethod.ToString()}");

                        if (attribute.LogicalOperation == LogicalOperation.And)
                            predicate.And(expression);
                        else if (attribute.LogicalOperation == LogicalOperation.Or)
                            predicate.Or(expression);
                        else
                            throw new FilterException($"Not supported LogicalOperation {attribute.LogicalOperation}");
                    }
                }
                else if (filterProperty.PropertyType == typeof(List<string>))
                {
                    var list = filterProperty.GetValue(this) as List<string>;

                    if (list != null && (list.Any() || !attribute.IgnoreIfEmpty))
                    {
                        Expression<Func<TModel, bool>> expression;
                        if (modelProperty.PropertyType == typeof(string))
                            expression = FilterExpressions<TModel>.GetListContainsExpression(propertyName, list, attribute.StringComparisonIgnoreCase);
                        else if (modelProperty.PropertyType == typeof(List<string>))
                            expression = FilterExpressions<TModel>.GetListAnyExpression(propertyName, list, attribute.StringComparisonIgnoreCase);
                        else
                        {
                            string error = $"Filter of type List<string> doesn't support Model property of type {modelProperty.PropertyType}." +
                                " You need to provide custom implementation for GetQueryExpression() method";
                            throw new FilterException(error);
                        }

                        if (attribute.LogicalOperation == LogicalOperation.And)
                            predicate.And(expression);
                        else if (attribute.LogicalOperation == LogicalOperation.Or)
                            predicate.Or(expression);
                        else
                            throw new FilterException($"Not supported LogicalOperation {attribute.LogicalOperation}");
                    }
                }
                else if (filterProperty.PropertyType == typeof(DateTime?))
                {
                    var date = filterProperty.GetValue(this) as DateTime?;
                    if (date.HasValue)
                    {
                        var value = modelProperty.PropertyType == typeof(DateTime) ? date.Value : date; 
                        var expression = FilterExpressions<TModel>.GetComparisonExpression(propertyName, value, attribute.ComparisonOperation);
                        predicate.And(expression);
                    }
                }
                else if (filterProperty.PropertyType == typeof(DateTime))
                {
                    var value = (DateTime)filterProperty.GetValue(this); 
                    var expression = FilterExpressions<TModel>.GetComparisonExpression(propertyName, value, attribute.ComparisonOperation);
                    predicate.And(expression);
                }
                else if (filterProperty.PropertyType == typeof(double?))
                {
                    var number = filterProperty.GetValue(this) as double?;
                    if (number.HasValue)
                    {
                        var value = modelProperty.PropertyType == typeof(double) ? number.Value : number;
                        var expression = FilterExpressions<TModel>.GetComparisonExpression(propertyName, value, attribute.ComparisonOperation);
                        predicate.And(expression);
                    }
                }
                else if (filterProperty.PropertyType == typeof(double))
                {
                    var value = (double)filterProperty.GetValue(this); 
                    var expression = FilterExpressions<TModel>.GetComparisonExpression(propertyName, value, attribute.ComparisonOperation);
                    predicate.And(expression);
                }
                else if (filterProperty.PropertyType == typeof(decimal?))
                {
                    var number = filterProperty.GetValue(this) as decimal?;
                    if (number.HasValue)
                    {
                        var value = modelProperty.PropertyType == typeof(decimal) ? number.Value : number;
                        var expression = FilterExpressions<TModel>.GetComparisonExpression(propertyName, value, attribute.ComparisonOperation);
                        predicate.And(expression);
                    }
                }
                else if (filterProperty.PropertyType == typeof(decimal))
                {
                    var value = (decimal)filterProperty.GetValue(this); 
                    var expression = FilterExpressions<TModel>.GetComparisonExpression(propertyName, value, attribute.ComparisonOperation);
                    predicate.And(expression);
                }
                else if (filterProperty.PropertyType == typeof(int?)) 
                {
                    var number = filterProperty.GetValue(this) as int?;
                    if (number.HasValue)
                    {
                        var value = modelProperty.PropertyType == typeof(int) ? number.Value : number;
                        var expression = FilterExpressions<TModel>.GetComparisonExpression(propertyName, value, attribute.ComparisonOperation);
                        predicate.And(expression);
                    }
                }
                else if (filterProperty.PropertyType == typeof(int)) 
                {
                    var value = (int)filterProperty.GetValue(this);
                    var expression = FilterExpressions<TModel>.GetComparisonExpression(propertyName, value, attribute.ComparisonOperation);
                    predicate.And(expression);
                }
                else
                {
                    var error = $"Filter property of type {filterProperty.PropertyType} is not supported." +
                                   " You need to GetQueryExpression() method to provide custom implementation";
                    throw new FilterException(error);
                }
            }
        }

        return predicate.IsStarted ? predicate : null;
    }

    private PropertyInfo GetModelProperty(string propertyPath)
    {
        var currentType = typeof(TModel);
        PropertyInfo propertyInfo = null;

        foreach (var item in propertyPath.Split('.'))
        {
            propertyInfo = currentType.GetProperty(item);
            if (propertyInfo == null)
                return null;

            currentType = propertyInfo.PropertyType;
        }

        return propertyInfo;
    }

    private static bool IsAllowedType(Type type) =>
        type == typeof(string) ||
        type == typeof(List<string>) ||
        type == typeof(DateTime) || type == typeof(DateTime?) ||
        type == typeof(double) || type == typeof(double?) ||
        type == typeof(decimal) || type == typeof(decimal?) ||
        type == typeof(int) || type == typeof(int?);

}
