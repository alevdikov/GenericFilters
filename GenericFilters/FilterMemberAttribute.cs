using System;

namespace GenericFilters
{
    public enum StringComparisonMethod
    {
        Equals, Contains
    }

    public enum ComparisonOperation
    {
        Equality, GreaterThan, GreaterThanOrEqual, Inequality, LessThan, LessThanOrEqual
    }

    public enum LogicalOperation
    {
        And, Or
    }

    [AttributeUsage(AttributeTargets.Property)]
    public class FilterMemberAttribute : Attribute
    {
        /// <summary>
        /// Provides a mapping between Filter property and Model property.
        /// </summary>
        public string Name { get; private set; }

        public StringComparison ComparisonType { get; private set; }

        /// <summary>
        /// Provides either '=' or LIKE %% functionality for strings inside Model, and strings inside Lists inside Model
        /// </summary>
        public StringComparisonMethod ComparisonMethod { get; private set; }

        /// <summary>
        /// Provides Comparison logic for DateTime types filters
        /// </summary>
        public ComparisonOperation ComparisonOperation { get; private set; }

        /// <summary>
        /// Pprovides filters linking logic, either And or Or. By default it uses And logic.
        /// In case you are going to apply Or logic, you need to be careful, it groups every next expression
        /// following the same order as properties are added in the class.
        /// So grouping of operations will be (((Prop1 Op Prop2) Op Prop3) Op Prop4)...   
        /// </summary>
        public LogicalOperation LogicalOperation { get; private set; }

        /// <summary>
        /// When set to true, the filter will not be included as part of query expression built by GetQueryExpression method,
        /// so you can build partial filter usinf default GetQueryExpression method and then add missing expressions in override. 
        /// </summary>
        public bool IgnoreInQueryExpression { get; private set; }

        /// <summary>
        /// When set to true, if property is null or empry, the property will not be included as part of query.
        /// This is default behaviour. Properties with null values are always excluded.
        /// This option is not applicable to DateTime properties and ignored. 
        /// </summary>
        public bool IgnoreIfEmpty { get; private set; }

        public FilterMemberAttribute(string name = null,
            StringComparison comparisonType = StringComparison.CurrentCulture,
            StringComparisonMethod comparisonMethod = StringComparisonMethod.Equals,
            ComparisonOperation comparisonOperation = ComparisonOperation.Equality,
            LogicalOperation logicalOperation = LogicalOperation.And,
            bool ignoreInQueryExpression = false,
            bool ignoreIfEmpty = true)
        {
            Name = name;
            ComparisonType = comparisonType;
            ComparisonMethod = comparisonMethod;
            ComparisonOperation = comparisonOperation;
            LogicalOperation = logicalOperation;
            IgnoreInQueryExpression = ignoreInQueryExpression;
            IgnoreIfEmpty = ignoreIfEmpty;
        }
    }
}
