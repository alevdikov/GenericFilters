using Xunit;

namespace GenericFilters.Tests;

public class FilterExpressions_Test
{
    #region String expressions methods tests

    [Theory]
    [InlineData("John", "John", false, true)]
    [InlineData("john", "John", false, false)]
    [InlineData("john", "John", true, true)]
    [InlineData("Jane", "John", true, false)]
    public void GetStringEqualsExpression_Test(
        string modelValue,
        string filterValue,
        bool ignoreCase,
        bool expectedResult) 
    {
        // Arrange
        var model = new TestModelWithString { Name = modelValue };
        var expression = FilterExpressions<TestModelWithString>.GetStringEqualsExpression("Name", filterValue, ignoreCase);
        var func = expression.Compile();

        // Act
        var result = func(model);

        // Assert
        Assert.Equal(expectedResult, result); 
    } 
    
    [Theory]
    [InlineData("John Doe", "John", false, true)]
    [InlineData("john Doe", "John", false, false)]
    [InlineData("john Doe", "John", true, true)]
    [InlineData("Jane Doe", "John", true, false)]
    public void GetStringContainsExpression_Test(
        string modelValue,
        string filterValue,
        bool ignoreCase,
        bool expectedResult)
    {
        // Arrange
        var model = new TestModelWithString { Name = modelValue };
        var expression = FilterExpressions<TestModelWithString>.GetStringContainsExpression("Name", filterValue, ignoreCase);
        var func = expression.Compile();

        // Act
        var result = func(model);

        // Assert
        Assert.Equal(expectedResult, result);
    }

    #endregion 

    #region string List expressions methods tests

    [Theory]
    [InlineData("Apple", new[] { "Apple", "Banana" }, false, true)]
    [InlineData("apple", new[] { "Apple", "Banana" }, false, false)]
    [InlineData("apple", new[] { "Apple", "Banana" }, true, true)]
    [InlineData("Cherry", new[] { "Apple", "Banana" }, true, false)]
    public void GetListContainsExpression_Test(
        string modelValue,
        string[] filterValues,
        bool ignoreCase,
        bool expectedResult)
    {
        // Arrange
        var model = new TestModelWithString { Name = modelValue };
        var filterList = new List<string>(filterValues);
        var expression = FilterExpressions<TestModelWithString>.GetListContainsExpression("Name", filterList, ignoreCase);
        var func = expression.Compile();

        // Act
        var result = func(model);

        // Assert
        Assert.Equal(expectedResult, result);
    }
    
    [Theory]
    [InlineData("Apple", new[] { "Apple", "Banana" }, false, true)]
    [InlineData("apple", new[] { "Apple", "Banana" }, false, false)]
    [InlineData("apple", new[] { "Apple", "Banana" }, true, true)]
    [InlineData("Cherry", new[] { "Apple", "Banana" }, true, false)]
    public void GetListContainsExpression_StringInput_Test(
        string input,
        string[] filterValues,
        bool ignoreCase,
        bool expectedResult)
    {
        // Arrange
        var filterList = new List<string>(filterValues);
        var expression = FilterExpressions<TestModelWithString>.GetListContainsExpression(filterList, ignoreCase);
        var func = expression.Compile();

        // Act
        var result = func(input);

        // Assert
        Assert.Equal(expectedResult, result);
    }
    
    [Theory]
    [InlineData(new[] { "Red", "Green", "Blue" }, new[] { "green", "yellow" }, false, false)]
    [InlineData(new[] { "Red", "Green", "Blue" }, new[] { "green", "yellow" }, true, true)]
    [InlineData(new[] { "Red", "Green", "Blue" }, new[] { "Blue" }, false, true)]
    [InlineData(new[] { "Red", "Green", "Blue" }, new[] { "Purple" }, true, false)]
    public void GetListAnyExpression_Test(
        string[] modelTags,
        string[] filterValues,
        bool ignoreCase,
        bool expectedResult)
    {
        // Arrange
        var model = new TestModelWithList { Tags = modelTags.ToList() };
        var filterList = filterValues.ToList();
        var expression = FilterExpressions<TestModelWithList>.GetListAnyExpression("Tags", filterList, ignoreCase);
        var func = expression.Compile();

        // Act
        var result = func(model);

        // Assert
        Assert.Equal(expectedResult, result);
    }
    
    #endregion
    
    #region Comparison expressions methods tests

    [Theory]
    [InlineData("2024-01-01", "2024-01-01", ComparisonOperation.Equality, true)]
    [InlineData("2024-01-01", "2023-12-31", ComparisonOperation.GreaterThan, true)]
    [InlineData("2024-01-01", "2023-12-31", ComparisonOperation.GreaterThanOrEqual, true)]
    [InlineData("2024-01-01", "2024-01-01", ComparisonOperation.GreaterThanOrEqual, true)]
    [InlineData("2024-01-01", "2024-01-02", ComparisonOperation.LessThan, true)]
    [InlineData("2024-01-01", "2024-01-02", ComparisonOperation.LessThanOrEqual, true)]
    [InlineData("2024-01-01", "2024-01-01", ComparisonOperation.LessThanOrEqual, true)]
    [InlineData("2024-01-01", "2024-01-01", ComparisonOperation.Inequality, false)]
    public void GetComparisonExpression_DateTime_Test(
        string modelDateStr,
        string filterDateStr,
        ComparisonOperation operation,
        bool expectedResult)
    {
        var model = new TestModelWithDate { Date = DateTime.Parse(modelDateStr) };
        var filterDate = DateTime.Parse(filterDateStr);

        var expression = FilterExpressions<TestModelWithDate>.GetComparisonExpression("Date", filterDate, operation);
        var func = expression.Compile();

        var result = func(model);

        Assert.Equal(expectedResult, result);
    }
    
    [Theory]
    [InlineData("2024-01-01", "2024-01-01", ComparisonOperation.Equality, true)]
    [InlineData("2024-01-01", "2023-12-31", ComparisonOperation.GreaterThan, true)]
    [InlineData("2024-01-01", "2023-12-31", ComparisonOperation.GreaterThanOrEqual, true)]
    [InlineData("2024-01-01", "2024-01-01", ComparisonOperation.GreaterThanOrEqual, true)]
    [InlineData("2024-01-01", "2024-01-02", ComparisonOperation.LessThan, true)]
    [InlineData("2024-01-01", "2024-01-02", ComparisonOperation.LessThanOrEqual, true)]
    [InlineData("2024-01-01", "2024-01-01", ComparisonOperation.LessThanOrEqual, true)]
    [InlineData("2024-01-01", "2024-01-01", ComparisonOperation.Inequality, false)]
    public void GetComparisonExpression_DateTime_Nullable_Test(
        string modelDateStr,
        string filterDateStr,
        ComparisonOperation operation,
        bool expectedResult)
    {
        var model = new TestModelWithDate { NullableDate = DateTime.Parse(modelDateStr) };
        var filterDate = DateTime.Parse(filterDateStr);

        var expression = FilterExpressions<TestModelWithDate>.GetComparisonExpression("NullableDate", filterDate, operation);
        var func = expression.Compile();

        var result = func(model);

        Assert.Equal(expectedResult, result);
    }
    
    [Theory]
    [InlineData(7.7, 7.7, ComparisonOperation.Equality, true)]
    [InlineData(7.8, 7.7, ComparisonOperation.GreaterThan, true)]
    [InlineData(7.8, 7.7, ComparisonOperation.GreaterThanOrEqual, true)]
    [InlineData(7.7, 7.7, ComparisonOperation.GreaterThanOrEqual, true)]
    [InlineData(7.6, 7.7, ComparisonOperation.LessThan, true)]
    [InlineData(7.6, 7.7, ComparisonOperation.LessThanOrEqual, true)]
    [InlineData(7.7, 7.7, ComparisonOperation.LessThanOrEqual, true)]
    [InlineData(7.7, 7.7, ComparisonOperation.Inequality, false)]
    public void GetComparisonExpression_Double_Test(
        double modelValue,
        double filterValue,
        ComparisonOperation operation,
        bool expectedResult)
    {
        var model = new TestModelWithDoubleNumber { Number = modelValue };
        
        var expression = FilterExpressions<TestModelWithDoubleNumber>.GetComparisonExpression("Number", filterValue, operation);
        var func = expression.Compile();

        var result = func(model);

        Assert.Equal(expectedResult, result);
    }
    
    [Theory]
    [InlineData(7.7, 7.7, ComparisonOperation.Equality, true)]
    [InlineData(7.8, 7.7, ComparisonOperation.GreaterThan, true)]
    [InlineData(7.8, 7.7, ComparisonOperation.GreaterThanOrEqual, true)]
    [InlineData(7.7, 7.7, ComparisonOperation.GreaterThanOrEqual, true)]
    [InlineData(7.6, 7.7, ComparisonOperation.LessThan, true)]
    [InlineData(7.6, 7.7, ComparisonOperation.LessThanOrEqual, true)]
    [InlineData(7.7, 7.7, ComparisonOperation.LessThanOrEqual, true)]
    [InlineData(7.7, 7.7, ComparisonOperation.Inequality, false)]
    public void GetComparisonExpression_Double_Nullable_Test(
        double? modelValue,
        double? filterValue,
        ComparisonOperation operation,
        bool expectedResult)
    {
        var model = new TestModelWithDoubleNumber { NullableNumber = modelValue };

        var expression = FilterExpressions<TestModelWithDoubleNumber>.GetComparisonExpression("NullableNumber", filterValue, operation);
        var func = expression.Compile();

        var result = func(model);

        Assert.Equal(expectedResult, result);
    }

    [Theory]
    [InlineData(7, 7, ComparisonOperation.Equality, true)]
    [InlineData(8, 7, ComparisonOperation.GreaterThan, true)]
    [InlineData(8, 7, ComparisonOperation.GreaterThanOrEqual, true)]
    [InlineData(7, 7, ComparisonOperation.GreaterThanOrEqual, true)]
    [InlineData(6, 7, ComparisonOperation.LessThan, true)]
    [InlineData(6, 7, ComparisonOperation.LessThanOrEqual, true)]
    [InlineData(7, 7, ComparisonOperation.LessThanOrEqual, true)]
    [InlineData(7, 7, ComparisonOperation.Inequality, false)]
    public void GetComparisonExpression_Integer_Test(
        int modelValue,
        int filterValue,
        ComparisonOperation operation,
        bool expectedResult)
    {
        var model = new TestModelWithIntegerNumber { Number = modelValue };
        
        var expression = FilterExpressions<TestModelWithIntegerNumber>.GetComparisonExpression("Number", filterValue, operation);
        var func = expression.Compile();

        var result = func(model);

        Assert.Equal(expectedResult, result);
    }
    
    [Theory]
    [InlineData(7, 7, ComparisonOperation.Equality, true)] 
    [InlineData(8, 7, ComparisonOperation.GreaterThan, true)]
    [InlineData(8, 7, ComparisonOperation.GreaterThanOrEqual, true)]
    [InlineData(7, 7, ComparisonOperation.GreaterThanOrEqual, true)]
    [InlineData(6, 7, ComparisonOperation.LessThan, true)]
    [InlineData(6, 7, ComparisonOperation.LessThanOrEqual, true)]
    [InlineData(7, 7, ComparisonOperation.LessThanOrEqual, true)]
    [InlineData(7, 7, ComparisonOperation.Inequality, false)]
    public void GetComparisonExpression_Integer_Nullable_Test(
        int? modelValue,
        int? filterValue,
        ComparisonOperation operation,
        bool expectedResult)
    {
        var model = new TestModelWithIntegerNumber { NullableNumber = modelValue };

        var expression = FilterExpressions<TestModelWithIntegerNumber>.GetComparisonExpression("NullableNumber", filterValue, operation);
        var func = expression.Compile();

        var result = func(model);

        Assert.Equal(expectedResult, result);
    }

    #endregion
    
    #region Test models

    private class TestModelWithString
    {
        public string Name { get; init; }
    }

    private class TestModelWithList
    {
        public List<string> Tags { get; init; }
    }
    
    private class TestModelWithDate
    {
        public DateTime Date { get; init; }
        public DateTime? NullableDate { get; init; }
    }

    private class TestModelWithDoubleNumber
    {
        public double Number { get; init; }
        public double? NullableNumber { get; init; }
    }

    private class TestModelWithIntegerNumber
    {
        public int Number { get; init; }
        public int? NullableNumber { get; init; }
    }

    #endregion
}
