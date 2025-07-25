using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace GenericFilters;

internal static class FilterExpressions<TModel> where TModel : class
{
    internal static Expression<Func<TModel, bool>> GetStringEqualsExpression(string propertyPath, string filterValue, bool stringComparisonIgnoreCase)
    {
        var parameter = Expression.Parameter(typeof(TModel), "x");
        var (propertyExpression, nullCheck) = GetNestedPropertyWithNullCheck(parameter, propertyPath);
        
        Expression comparison;
        var methodInfo = typeof(string).GetMethod("Equals", [ typeof(string) ]);

        if (stringComparisonIgnoreCase)
        {
            var toLower = Expression.Call(propertyExpression, typeof(string).GetMethod("ToLower", Type.EmptyTypes));
            var value = Expression.Constant(filterValue.ToLower(), typeof(string));
            comparison = Expression.Call(toLower, methodInfo, value);
        }
        else
        {
            var value = Expression.Constant(filterValue, typeof(string));
            comparison = Expression.Call(propertyExpression, methodInfo, value);
        }

        var body = nullCheck != null ? Expression.AndAlso(nullCheck, comparison) : comparison;

        return Expression.Lambda<Func<TModel, bool>>(body, parameter);
    }

    internal static Expression<Func<TModel, bool>> GetStringContainsExpression(string propertyPath, string filterValue, bool stringComparisonIgnoreCase)
    {
        var parameter = Expression.Parameter(typeof(TModel), "x");
        var (propertyExpression, nullCheck) = GetNestedPropertyWithNullCheck(parameter, propertyPath);

        Expression comparison;
        var methodInfo = typeof(string).GetMethod("Contains", [ typeof(string) ]);

        if (stringComparisonIgnoreCase)
        {
            var toLower = Expression.Call(propertyExpression, typeof(string).GetMethod("ToLower", Type.EmptyTypes));
            var value = Expression.Constant(filterValue.ToLower(), typeof(string));
            comparison = Expression.Call(toLower, methodInfo, value);
        }
        else
        {
            var value = Expression.Constant(filterValue, typeof(string));
            comparison = Expression.Call(propertyExpression, methodInfo, value);
        }

        var body = nullCheck != null ? Expression.AndAlso(nullCheck, comparison) : comparison;

        return Expression.Lambda<Func<TModel, bool>>(body, parameter);
    }
    
    internal static Expression<Func<TModel, bool>> GetListContainsExpression(string propertyPath, List<string> filterValue, bool stringComparisonIgnoreCase)
    {
        var parameter = Expression.Parameter(typeof(TModel), "x");
        var (propertyExpression, nullCheck) = GetNestedPropertyWithNullCheck(parameter, propertyPath);

        if (stringComparisonIgnoreCase)
            filterValue = filterValue.ConvertAll(s => s.ToLower());

        var methodInfo = typeof(List<string>).GetMethod("Contains", [ typeof(string) ]);

        Expression target = stringComparisonIgnoreCase
            ? Expression.Call(propertyExpression, typeof(string).GetMethod("ToLower", Type.EmptyTypes))
            : propertyExpression;

        var containsCall = Expression.Call(Expression.Constant(filterValue), methodInfo, target);

        Expression body = nullCheck != null
            ? (Expression)Expression.AndAlso(nullCheck, containsCall)
            : containsCall;

        return Expression.Lambda<Func<TModel, bool>>(body, parameter);
    }
    
    internal static Expression<Func<string, bool>> GetListContainsExpression(List<string> filterValue, bool stringComparisonIgnoreCase)
    {
        var parameterExpression = Expression.Parameter(typeof(string));

        var methodInfo = typeof(List<string>).GetMethod("Contains", [ typeof(string) ]);

        MethodCallExpression expression;
        if (!stringComparisonIgnoreCase)
        {
            var value = Expression.Constant(filterValue);
            expression = Expression.Call(value, methodInfo, parameterExpression);
        }
        else
        {
            var toLowerExpression = Expression.Call(parameterExpression, typeof(string).GetMethod("ToLower", Type.EmptyTypes));
            var value = Expression.Constant(filterValue.ConvertAll(i => i.ToLower()));
            expression = Expression.Call(value, methodInfo, toLowerExpression);
        }

        var lambda = Expression.Lambda<Func<string, bool>>(expression, parameterExpression);

        return lambda;
    }

    internal static Expression<Func<TModel, bool>> GetListAnyExpression(string propertyPath, List<string> filterValue, bool stringComparisonIgnoreCase)
    {
        var parameter = Expression.Parameter(typeof(TModel), "x");
        var (propertyExpression, nullCheck) = GetNestedPropertyWithNullCheck(parameter, propertyPath);

        var elementType = typeof(string);
        var itemParam = Expression.Parameter(elementType, "s");

        Expression itemExpr = stringComparisonIgnoreCase
            ? Expression.Call(itemParam, typeof(string).GetMethod("ToLower", Type.EmptyTypes))
            : itemParam;

        if (stringComparisonIgnoreCase)
            filterValue = filterValue.ConvertAll(s => s.ToLower());

        var filterList = Expression.Constant(filterValue);
        var containsMethod = typeof(List<string>).GetMethod("Contains", [ typeof(string) ]);
        var containsCall = Expression.Call(filterList, containsMethod, itemExpr);

        var predicate = Expression.Lambda(containsCall, itemParam);

        var anyMethod = typeof(Enumerable)
            .GetMethods(BindingFlags.Static | BindingFlags.Public)
            .First(m => m.Name == "Any" && m.GetParameters().Length == 2)
            .MakeGenericMethod(elementType);

        var anyCall = Expression.Call(anyMethod, propertyExpression, predicate);

        Expression body = nullCheck != null
            ? Expression.AndAlso(nullCheck, anyCall)
            : anyCall;

        return Expression.Lambda<Func<TModel, bool>>(body, parameter);
    }

    internal static Expression<Func<TModel, bool>> GetComparisonExpression<TProperty>(string propertyPath, TProperty filterValue, ComparisonOperation operation)
    {
        var parameter = Expression.Parameter(typeof(TModel), "x");
        var (propertyExpression, nullCheck) = GetNestedPropertyWithNullCheck(parameter, propertyPath);

        var value = Expression.Constant(filterValue, typeof(TProperty));

        var propertyType = Nullable.GetUnderlyingType(propertyExpression.Type) ?? propertyExpression.Type;
        var filterType = Nullable.GetUnderlyingType(typeof(TProperty)) ?? typeof(TProperty);

        if (!propertyType.IsAssignableFrom(filterType))
        {
            throw new ArgumentException($"Property '{propertyPath}' is not compatible with type {typeof(TProperty).Name}");
        }

        Expression left = propertyExpression;
        Expression right = value;
        if (propertyExpression.Type != typeof(TProperty))
        {
            left = Expression.Convert(propertyExpression, propertyType);
            right = Expression.Convert(value, propertyType);
        }

        BinaryExpression comparison = operation switch
        {
            ComparisonOperation.Equality => Expression.Equal(left, right),
            ComparisonOperation.Inequality => Expression.NotEqual(left, right),
            ComparisonOperation.GreaterThan => Expression.GreaterThan(left, right),
            ComparisonOperation.GreaterThanOrEqual => Expression.GreaterThanOrEqual(left, right),
            ComparisonOperation.LessThan => Expression.LessThan(left, right),
            ComparisonOperation.LessThanOrEqual => Expression.LessThanOrEqual(left, right),
            _ => throw new NotImplementedException($"Operation {operation} is not supported")
        };

        Expression body = nullCheck != null
            ? (Expression)Expression.AndAlso(nullCheck, comparison)
            : comparison;

        return Expression.Lambda<Func<TModel, bool>>(body, parameter);
    }

    #region Helper methods

    private static (Expression propertyExpression, Expression nullCheck) GetNestedPropertyWithNullCheck(
        ParameterExpression parameter,
        string propertyPath)
    {
        Expression current = parameter;
        Expression nullCheck = null;

        var properties = propertyPath.Split('.');
        for (int i = 0; i < properties.Length; i++)
        {
            var propertyName = properties[i];
            var property = Expression.PropertyOrField(current, properties[i]);

            // Skip root parameter and add null check for reference types
            if (i > 0 && current.Type.IsClass)
            {
                var check = Expression.NotEqual(current, Expression.Constant(null, current.Type));
                nullCheck = nullCheck == null ? check : Expression.AndAlso(nullCheck, check);
            }

            current = property;
        }

        return (current, nullCheck);
    }
    
    #endregion
}
