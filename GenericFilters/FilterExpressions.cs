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

    internal static Expression<Func<TModel, bool>> GetComparisonExpression<TProperty>(
        string propertyName,
        TProperty filterValue,
        ComparisonOperation operation)
    {
        var parameter = Expression.Parameter(typeof(TModel), "x");
        var property = Expression.Property(parameter, propertyName);
        var value = Expression.Constant(filterValue, typeof(TProperty));

        // Get the underlying type if nullable
        var propertyType = Nullable.GetUnderlyingType(property.Type) ?? property.Type;
        var filterType = Nullable.GetUnderlyingType(typeof(TProperty)) ?? typeof(TProperty);

        // Ensure types are compatible
        if (!propertyType.IsAssignableFrom(filterType))
        {
            throw new ArgumentException($"Property '{propertyName}' is not compatible with type {typeof(TProperty).Name}");
        }

        // Convert both sides to the same non-nullable type
        Expression left = property;
        Expression right = value;
        if (property.Type != typeof(TProperty))
        {
            left = Expression.Convert(property, propertyType);
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

        return Expression.Lambda<Func<TModel, bool>>(comparison, parameter);
    }
}
