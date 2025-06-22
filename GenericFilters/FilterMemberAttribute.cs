using System;

namespace GenericFilters;

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

    /// <summary>
    /// Provides either '=' or LIKE %% functionality for string properties inside the Model,
    /// and strings inside List properties inside the Model
    /// NOTE: This property doesn't work with EntityFramework when you are using IQueryable.
    /// It works with regular Linq collections, and CosmosDb SDK getItemLinqQueryable method 
    /// </summary>
    public StringComparisonMethod StringComparisonMethod { get; private set; }

    /// <summary>
    /// When is true, converts strings to lower case on both sides, the Model and the Filter.  
    /// We don't apply System.StringComparison used for Equals and Contains operations,
    /// since it is not applicable to EntityFramework when IQueryable is returned. 
    /// Applicable for both String and List types.
    /// NOTE: If you are using Filter with EntityFramework or any another ORM, make sure you database
    /// has case-sensitive collation. Because applying tha conversion may have negative impact
    /// on query performance.
    /// </summary>
    public bool StringComparisonIgnoreCase { get; private set; }

    /// <summary>
    /// Provides Comparison logic for DateTime types filters
    /// </summary>
    public ComparisonOperation ComparisonOperation { get; private set; }

    /// <summary>
    /// Provides filters linking logic, either 'And' or 'Or'. By default, it uses 'And' logic.
    /// In case you are going to apply 'Or' logic, you need to be careful, it groups every next expression
    /// following the same order as properties are added in the class.
    /// So grouping of operations will be (((Prop1 Op Prop2) Op Prop3) Op Prop4)...   
    /// </summary>
    public LogicalOperation LogicalOperation { get; private set; }

    /// <summary>
    /// When set to true, the filter will not be included as part of the query expression built by GetQueryExpression method,
    /// so you can build a partial filter using default GetQueryExpression method, and then add missing expressions in the override. 
    /// </summary>
    public bool IgnoreInQueryExpression { get; private set; }

    /// <summary>
    /// When set to true, if property is null or empty, the property will not be included as part of the query.
    /// This is a default behaviour. Properties with null values are always excluded.
    /// This option is not applicable to DateTime properties and ignored. 
    /// </summary>
    public bool IgnoreIfEmpty { get; private set; }

    public FilterMemberAttribute(string name = null,
        StringComparisonMethod stringComparisonMethod = StringComparisonMethod.Equals,
        bool stringComparisonIgnoreCase = false,
        ComparisonOperation comparisonOperation = ComparisonOperation.Equality,
        LogicalOperation logicalOperation = LogicalOperation.And,
        bool ignoreInQueryExpression = false,
        bool ignoreIfEmpty = true)
    {
        Name = name;
        StringComparisonMethod = stringComparisonMethod;
        StringComparisonIgnoreCase = stringComparisonIgnoreCase;
        ComparisonOperation = comparisonOperation;
        LogicalOperation = logicalOperation;
        IgnoreInQueryExpression = ignoreInQueryExpression;
        IgnoreIfEmpty = ignoreIfEmpty;
    }
}
