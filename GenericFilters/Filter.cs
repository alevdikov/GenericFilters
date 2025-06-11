using LinqKit;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace GenericFilters
{
    public abstract class Filter<TModel> where TModel : class
    {
        public int StartingIndex { get; set; } = -1;
        public int PageSize { get; set; } = -1;

        public Filter()
        {
            var isValid = false;

            foreach (var property in GetType().GetProperties())
            {
                var hasFilterAttribute = Attribute.IsDefined(property, typeof(FilterMemberAttribute));
                if (hasFilterAttribute)
                {
                    isValid = true;
                    if (property.PropertyType == typeof(string))
                        continue;
                    if (property.PropertyType == typeof(List<string>))
                        continue;
                    if (property.PropertyType == typeof(DateTime) || property.PropertyType == typeof(DateTime?))
                        continue;
                    else
                        throw new FilterException($"Filter member with type {property.PropertyType} is not supported.");
                }
            }

            if (!isValid)
                throw new FilterException("You need to specify at least one Filter element using FilterMember attribute");
        }

        public override int GetHashCode()
        {
            int hashCode = 0;

            foreach (var property in GetType().GetProperties())
            {
                var hasFilterAttribute = Attribute.IsDefined(property, typeof(FilterMemberAttribute));
                if (hasFilterAttribute)
                {
                    if (property.PropertyType == typeof(string))
                    {
                        if (property.GetValue(this) != null)
                            hashCode ^= property.GetValue(this).ToString().GetHashCode();
                    }
                    else if (property.PropertyType == typeof(List<string>))
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
                    else
                    {
                        string error = $"Filter member with type {property.PropertyType} is not supported." +
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
                    if (property.PropertyType == typeof(string))
                    {
                        if (!string.IsNullOrEmpty(property.GetValue(this) as string))
                            return true;
                    }
                    if (property.PropertyType == typeof(List<string>))
                    {
                        var list = property.GetValue(this) as List<string>;
                        if (list != null && list.Any())
                            return true;
                    }
                    if (property.PropertyType == typeof(DateTime?))
                    {
                        var date = property.GetValue(this) as DateTime?;
                        if (date != null)
                            return true;
                    }
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
                    if (property.PropertyType == typeof(string))
                    {
                        if (string.IsNullOrEmpty(property.GetValue(this) as string))
                            return false;
                    }
                    if (property.PropertyType == typeof(List<string>))
                    {
                        var list = property.GetValue(this) as List<string>;
                        if (list is null || !list.Any())
                            return false;
                    }
                    if (property.PropertyType == typeof(DateTime?))
                    {
                        var date = property.GetValue(this) as DateTime?;
                        if (date is null)
                            return false;
                    }
                }
            }

            return true;
        }

        public virtual Expression<Func<TModel, bool>> GetQueryExpression(FilterOptions filterOptions = null)
        {
            var predicate = PredicateBuilder.New<TModel>();

            foreach (var filterProperty in GetType().GetProperties())
            {
                var attribute = filterProperty.GetCustomAttribute(typeof(FilterMemberAttribute)) as FilterMemberAttribute;
                if (attribute != null)
                {
                    if (attribute.IgnoreInQueryExpression) continue;

                    var propertyName = attribute.Name ?? filterProperty.Name;
                    var comarisonType = attribute.ComparisonType;

                    var modelProperty = typeof(TModel).GetProperty(propertyName);
                    if (modelProperty == null)
                    {
                        if (filterOptions == null || !filterOptions.Optimistic)
                        {
                            string error = $"Model doesn't contain property with name {propertyName} required by Filter." +
                              " You need to add that property, decorate it in the Filter using FilterMember attribute," +
                              " or provide custom implementation for GetQueryExpression() method";
                            throw new FilterException(error);
                        }
                        else continue;
                    }

                    if (filterProperty.PropertyType == typeof(string))
                    {
                        var strValue = filterProperty.GetValue(this) as string;
                        if (strValue != null && (strValue != "" || !attribute.IgnoreIfEmpty))
                        {
                            Expression<Func<TModel, bool>> expression;
                            if (attribute.ComparisonMethod == StringComparisonMethod.Equals)
                                expression = GetStringEqualsExpression(propertyName, strValue, attribute.ComparisonType);
                            else if (attribute.ComparisonMethod == StringComparisonMethod.Contains)
                                expression = GetStringContainsExpression(propertyName, strValue, attribute.ComparisonType);
                            else
                                throw new FilterException($"Illegal strings comparison method: {attribute.ComparisonMethod.ToString()}");

                            if (attribute.LogicalOperation == LogicalOperation.And)
                                predicate.And(expression);
                            else if (attribute.LogicalOperation == LogicalOperation.Or)
                                predicate.Or(expression);
                            else
                                throw new FilterException($"Not supported LogicalOperation {attribute.LogicalOperation}");
                        }
                    }
                    if (filterProperty.PropertyType == typeof(List<string>))
                    {
                        var list = filterProperty.GetValue(this) as List<string>;

                        if (list != null && (list.Any() || !attribute.IgnoreIfEmpty))
                        {
                            Expression<Func<TModel, bool>> expression;
                            if (modelProperty.PropertyType == typeof(string))
                                expression = GetListContainsExpression(propertyName, list, attribute.ComparisonType);
                            else if (modelProperty.PropertyType == typeof(List<string>))
                                expression = GetListAnyExpression(propertyName, list, attribute.ComparisonType);
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
                    if (filterProperty.PropertyType == typeof(DateTime?))
                    {
                        var date = filterProperty.GetValue(this) as DateTime?;
                        if (date.HasValue)
                        {
                            if (modelProperty.PropertyType == typeof(DateTime))
                            {
                                var expression = GetDateExpression(propertyName, date.Value, attribute.ComparisonOperation);
                                predicate.And(expression);
                            }
                            else if (modelProperty.PropertyType == typeof(DateTime?))
                            {
                                var expression = GetDateNullableExpression(propertyName, date, attribute.ComparisonOperation);
                                predicate.And(expression);
                            }
                            else
                            {
                                string error = $"Filter of type DateTime? doesn't support Model property of type {modelProperty.PropertyType}." +
                                    " You need to provide custom implementation for GetQueryExpression() method";
                                throw new FilterException(error);
                            }
                        }
                    }
                }
            }

            return predicate.IsStarted ? predicate : null;
        }

        #region Create lambda expressions methods

        private static Expression<Func<TModel, bool>> GetStringEqualsExpression(string propertyName, string filterValue, StringComparison comparisonType)
        {
            var parameterExpression = Expression.Parameter(typeof(TModel));
            var property = Expression.Property(parameterExpression, propertyName);

            var methodInfo = typeof(string).GetMethod("Equals", new[] { typeof(string), typeof(StringComparison) });

            var value = Expression.Constant(filterValue);
            var expression = Expression.Call(property, methodInfo, value, Expression.Constant(comparisonType));

            var lambda = Expression.Lambda<Func<TModel, bool>>(expression, parameterExpression);

            return lambda;
        }

        private static Expression<Func<TModel, bool>> GetStringContainsExpression(string propertyName, string filterValue, StringComparison comparisonType)
        {
            var parameterExpression = Expression.Parameter(typeof(TModel));
            var property = Expression.Property(parameterExpression, propertyName);

            var methodInfo = typeof(string).GetMethod("Contains", new[] { typeof(string), typeof(StringComparison) });

            var value = Expression.Constant(filterValue);
            var expression = Expression.Call(property, methodInfo, value, Expression.Constant(comparisonType));

            var lambda = Expression.Lambda<Func<TModel, bool>>(expression, parameterExpression);

            return lambda;
        }

        private static Expression<Func<TModel, bool>> GetListContainsExpression(string propertyName, List<string> filterValue, StringComparison comparisonType)
        {
            Func<bool> isCaseInsensitive = () => comparisonType == StringComparison.OrdinalIgnoreCase ||
                comparisonType == StringComparison.CurrentCultureIgnoreCase ||
                comparisonType == StringComparison.InvariantCultureIgnoreCase;

            var parameterExpression = Expression.Parameter(typeof(TModel));
            var property = Expression.Property(parameterExpression, propertyName);

            var methodInfo = typeof(List<string>).GetMethod("Contains", new[] { typeof(string) });

            MethodCallExpression expression;
            if (isCaseInsensitive())
            {
                var toLowerExpression = Expression.Call(property, typeof(string).GetMethod("ToLower", Type.EmptyTypes));
                var value = Expression.Constant(filterValue.ConvertAll(i => i.ToLower()));
                expression = Expression.Call(value, methodInfo, toLowerExpression);
            }
            else
            {
                var value = Expression.Constant(filterValue);
                expression = Expression.Call(value, methodInfo, property);
            }

            var lambda = Expression.Lambda<Func<TModel, bool>>(expression, parameterExpression);

            return lambda;
        }

        private static Expression<Func<string, bool>> GetListContainsExpression(List<string> filterValue, StringComparison comparisonType)
        {
            Func<bool> isCaseInsensitive = () => comparisonType == StringComparison.OrdinalIgnoreCase ||
                comparisonType == StringComparison.CurrentCultureIgnoreCase ||
                comparisonType == StringComparison.InvariantCultureIgnoreCase;

            var parameterExpression = Expression.Parameter(typeof(string));

            var methodInfo = typeof(List<string>).GetMethod("Contains", new[] { typeof(string) });

            MethodCallExpression expression;
            if (isCaseInsensitive())
            {
                var toLowerExpression = Expression.Call(parameterExpression, typeof(string).GetMethod("ToLower", Type.EmptyTypes));
                var value = Expression.Constant(filterValue.ConvertAll(i => i.ToLower()));
                expression = Expression.Call(value, methodInfo, toLowerExpression);
            }
            else
            {
                var value = Expression.Constant(filterValue);
                expression = Expression.Call(value, methodInfo, parameterExpression);
            }

            var lambda = Expression.Lambda<Func<string, bool>>(expression, parameterExpression);

            return lambda;
        }

        private static Expression<Func<TModel, bool>> GetListAnyExpression(string propertyName, List<string> filterValue, StringComparison comparisonType)
        {
            var parameterExpression = Expression.Parameter(typeof(TModel));
            var property = Expression.Property(parameterExpression, propertyName);

            var methodInfo = typeof(Enumerable)
                .GetMethods(BindingFlags.Static | BindingFlags.Public)
                .Where(x => x.Name.Contains("Any"))
                .Single(x => x.GetParameters().Length == 2)
                .MakeGenericMethod(typeof(string));

            var containsExpression = GetListContainsExpression(filterValue, comparisonType);

            var expression = Expression.Call(methodInfo, property, containsExpression);

            var lambda = Expression.Lambda<Func<TModel, bool>>(expression, parameterExpression);

            return lambda;
        }

        private static Expression<Func<TModel, bool>> GetDateExpression(string propertyName, DateTime filterValue, ComparisonOperation operation)
        {
            var parameterExpression = Expression.Parameter(typeof(TModel));
            var property = Expression.Property(parameterExpression, propertyName);
            var value = Expression.Constant(filterValue, typeof(DateTime));

            BinaryExpression expression;

            switch (operation)
            {
                case ComparisonOperation.Equality:
                    expression = Expression.Equal(value, property);
                    break;
                case ComparisonOperation.GreaterThan:
                    expression = Expression.GreaterThan(value, property);
                    break;
                case ComparisonOperation.GreaterThanOrEqual:
                    expression = Expression.GreaterThanOrEqual(value, property);
                    break;
                case ComparisonOperation.Inequality:
                    expression = Expression.NotEqual(value, property);
                    break;
                case ComparisonOperation.LessThan:
                    expression = Expression.LessThan(value, property);
                    break;
                case ComparisonOperation.LessThanOrEqual:
                    expression = Expression.LessThanOrEqual(value, property);
                    break;
                default:
                    throw new NotImplementedException($"Operation {operation} is not supported");
            }

            var lambda = Expression.Lambda<Func<TModel, bool>>(expression, parameterExpression);

            return lambda;
        }

        private static Expression<Func<TModel, bool>> GetDateNullableExpression(string propertyName, DateTime? filterValue, ComparisonOperation operation)
        {
            var parameterExpression = Expression.Parameter(typeof(TModel));
            var property = Expression.Property(parameterExpression, propertyName);
            var value = Expression.Constant(filterValue, typeof(DateTime?));

            BinaryExpression expression;

            switch (operation)
            {
                case ComparisonOperation.Equality:
                    expression = Expression.Equal(value, property);
                    break;
                case ComparisonOperation.GreaterThan:
                    expression = Expression.GreaterThan(value, property);
                    break;
                case ComparisonOperation.GreaterThanOrEqual:
                    expression = Expression.GreaterThanOrEqual(value, property);
                    break;
                case ComparisonOperation.Inequality:
                    expression = Expression.NotEqual(value, property);
                    break;
                case ComparisonOperation.LessThan:
                    expression = Expression.LessThan(value, property);
                    break;
                case ComparisonOperation.LessThanOrEqual:
                    expression = Expression.LessThanOrEqual(value, property);
                    break;
                default:
                    throw new NotImplementedException($"Operation {operation} is not supported");
            }

            var lambda = Expression.Lambda<Func<TModel, bool>>(expression, parameterExpression);

            return lambda;
        }

        #endregion

    }
}
