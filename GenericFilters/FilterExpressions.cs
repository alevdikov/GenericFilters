using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace GenericFilters;

internal static class FilterExpressions<TModel> where TModel : class
{
    internal static Expression<Func<TModel, bool>> GetStringEqualsExpression(string propertyName, string filterValue, bool stringComparisonIgnoreCase)
    {
        var parameterExpression = Expression.Parameter(typeof(TModel));
        var property = Expression.Property(parameterExpression, propertyName);

        var methodInfo = typeof(string).GetMethod("Equals", [ typeof(string) ]);

        MethodCallExpression expression;
        if (!stringComparisonIgnoreCase)
        {
            var value = Expression.Constant(filterValue);
            expression = Expression.Call(property, methodInfo, value);
        }
        else
        {
            var toLowerExpression = Expression.Call(property, typeof(string).GetMethod("ToLower", Type.EmptyTypes));
            var value = Expression.Constant(filterValue.ToLower());
            expression = Expression.Call(toLowerExpression, methodInfo, value);
        }

        var lambda = Expression.Lambda<Func<TModel, bool>>(expression, parameterExpression);

        return lambda;
    }

    internal static Expression<Func<TModel, bool>> GetStringContainsExpression(string propertyName, string filterValue, bool stringComparisonIgnoreCase)
    {
        var parameterExpression = Expression.Parameter(typeof(TModel));
        var property = Expression.Property(parameterExpression, propertyName);

        var methodInfo = typeof(string).GetMethod("Contains", [ typeof(string) ]);

        MethodCallExpression expression;
        if (!stringComparisonIgnoreCase)
        {
            var value = Expression.Constant(filterValue);
            expression = Expression.Call(property, methodInfo, value);
        }
        else
        {
            var toLowerExpression = Expression.Call(property, typeof(string).GetMethod("ToLower", Type.EmptyTypes));
            var value = Expression.Constant(filterValue.ToLower());
            expression = Expression.Call(toLowerExpression, methodInfo, value);
        }

        var lambda = Expression.Lambda<Func<TModel, bool>>(expression, parameterExpression);

        return lambda;
    }

    internal static Expression<Func<TModel, bool>> GetListContainsExpression(string propertyName, List<string> filterValue, bool stringComparisonIgnoreCase)
    {
        var parameterExpression = Expression.Parameter(typeof(TModel));
        var property = Expression.Property(parameterExpression, propertyName);

        var methodInfo = typeof(List<string>).GetMethod("Contains", [ typeof(string) ]);

        MethodCallExpression expression;
        if (!stringComparisonIgnoreCase)
        {
            var value = Expression.Constant(filterValue);
            expression = Expression.Call(value, methodInfo, property);
        }
        else
        {
            var toLowerExpression = Expression.Call(property, typeof(string).GetMethod("ToLower", Type.EmptyTypes));
            var value = Expression.Constant(filterValue.ConvertAll(i => i.ToLower()));
            expression = Expression.Call(value, methodInfo, toLowerExpression);
        }

        var lambda = Expression.Lambda<Func<TModel, bool>>(expression, parameterExpression);

        return lambda;
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

    internal static Expression<Func<TModel, bool>> GetListAnyExpression(string propertyName, List<string> filterValue, bool stringComparisonIgnoreCase)
    {
        var parameterExpression = Expression.Parameter(typeof(TModel));
        var property = Expression.Property(parameterExpression, propertyName);

        var methodInfo = typeof(Enumerable)
            .GetMethods(BindingFlags.Static | BindingFlags.Public)
            .Where(x => x.Name.Contains("Any"))
            .Single(x => x.GetParameters().Length == 2)
            .MakeGenericMethod(typeof(string));

        var containsExpression = GetListContainsExpression(filterValue, stringComparisonIgnoreCase);

        var expression = Expression.Call(methodInfo, property, containsExpression);

        var lambda = Expression.Lambda<Func<TModel, bool>>(expression, parameterExpression);

        return lambda;
    }

    internal static Expression<Func<TModel, bool>> GetDateExpression(string propertyName, DateTime filterValue, ComparisonOperation operation)
    {
        var parameterExpression = Expression.Parameter(typeof(TModel));
        var property = Expression.Property(parameterExpression, propertyName);
        var value = Expression.Constant(filterValue, typeof(DateTime));

        BinaryExpression expression;

        switch (operation)
        {
            case ComparisonOperation.Equality:
                expression = Expression.Equal(property, value);
                break;
            case ComparisonOperation.GreaterThan:
                expression = Expression.GreaterThan(property, value);
                break;
            case ComparisonOperation.GreaterThanOrEqual:
                expression = Expression.GreaterThanOrEqual(property, value);
                break;
            case ComparisonOperation.Inequality:
                expression = Expression.NotEqual(property, value);
                break;
            case ComparisonOperation.LessThan:
                expression = Expression.LessThan(property, value);
                break;
            case ComparisonOperation.LessThanOrEqual:
                expression = Expression.LessThanOrEqual(property, value);
                break;
            default:
                throw new NotImplementedException($"Operation {operation} is not supported");
        }

        var lambda = Expression.Lambda<Func<TModel, bool>>(expression, parameterExpression);
        
        return lambda;
    }

    internal static Expression<Func<TModel, bool>> GetDateNullableExpression(string propertyName, DateTime? filterValue, ComparisonOperation operation)
    {
        var parameterExpression = Expression.Parameter(typeof(TModel));
        var property = Expression.Property(parameterExpression, propertyName);
        var value = Expression.Constant(filterValue, typeof(DateTime?));

        BinaryExpression expression;

        switch (operation)
        {
            case ComparisonOperation.Equality:
                expression = Expression.Equal(property, value);
                break;
            case ComparisonOperation.GreaterThan:
                expression = Expression.GreaterThan(property, value);
                break;
            case ComparisonOperation.GreaterThanOrEqual:
                expression = Expression.GreaterThanOrEqual(property, value);
                break;
            case ComparisonOperation.Inequality:
                expression = Expression.NotEqual(property, value);
                break;
            case ComparisonOperation.LessThan:
                expression = Expression.LessThan(property, value);
                break;
            case ComparisonOperation.LessThanOrEqual:
                expression = Expression.LessThanOrEqual(property, value);
                break;
            default:
                throw new NotImplementedException($"Operation {operation} is not supported");
        }

        var lambda = Expression.Lambda<Func<TModel, bool>>(expression, parameterExpression);

        return lambda;
    }
}
