using System.Globalization;
using Xunit;

namespace GenericFilters.Tests;

public class FilterTest
{
    [Fact]
    public void GetHashCode_Equal_Test()
    {
        #region Prepare data

        var date = DateTime.Now;

        var filter1 = new TestFilter1
        {
            Id = "1",
            Name = "Test 1",
            Prop = "Prop 1",
            Date = date,
            Items1 = new List<string> { "1", "2", "3" },
            Items2 = new List<string> { "4", "5", "6" },
            Optional = "Optional 1"
        };

        var filter2 = new TestFilter1
        {
            Id = "1",
            Name = "Test 1",
            Prop = "Prop 1",
            Date = date,
            Items1 = new List<string> { "1", "2", "3" },
            Items2 = new List<string> { "4", "5", "6" },
            Optional = "Optional 2"
        };

        var filter3 = new TestFilter1
        {
            Id = "1",
            Name = "Test 1",
            Prop = "Prop 1",
            Date = date,
            Items1 = new List<string> { "3", "2", "1" },
            Items2 = new List<string> { "6", "5", "4" },
            Optional = "Optional 1"
        };

        #endregion

        var hash1 = filter1.GetHashCode();
        var hash2 = filter2.GetHashCode();
        var hash3 = filter3.GetHashCode();

        Assert.Equal(hash1, hash2);
        Assert.Equal(hash1, hash3);
    }

    [Fact]
    public void GetHashCode_NotEqual_Test()
    {
        #region Prepare data

        var date1 = DateTime.Now;
        var date2 = DateTime.Now.AddDays(1);

        var filter1 = new TestFilter1
        {
            Id = "1",
            Name = "Test 1",
            Prop = "Prop 1",
            Date = date1,
            Items1 = new List<string> { "1", "2", "3" },
            Items2 = new List<string> { "4", "5", "6" },
            Optional = "Optional 1"
        };

        var filter2 = new TestFilter1
        {
            Id = "2",
            Name = "Test 1",
            Prop = "Prop 1",
            Date = date1,
            Items1 = new List<string> { "1", "2", "3" },
            Items2 = new List<string> { "4", "5", "6" },
            Optional = "Optional 2"
        };

        var filter3 = new TestFilter1
        {
            Id = "1",
            Name = "Test 1",
            Prop = "Prop 1",
            Date = date1,
            Items1 = new List<string> { "1", "2", "3" },
            Items2 = new List<string> { "4", "5", "7" },
            Optional = "Optional 1"
        };

        var filter4 = new TestFilter1
        {
            Id = "1",
            Name = "Test 1",
            Prop = "Prop 1",
            Date = date2,
            Items1 = new List<string> { "1", "2", "3" },
            Items2 = new List<string> { "4", "5", "6" },
            Optional = "Optional 1"
        };

        #endregion

        var hash1 = filter1.GetHashCode();
        var hash2 = filter2.GetHashCode();
        var hash3 = filter3.GetHashCode();
        var hash4 = filter4.GetHashCode();

        Assert.NotEqual(hash1, hash2);
        Assert.NotEqual(hash1, hash3);
        Assert.NotEqual(hash1, hash4);
    }

    [Fact]
    public void Any_Test()
    {
        #region Prepare data

        var filter1 = new TestFilter1
        {
            Id = "1",
        };

        var filter2 = new TestFilter1
        {
            Optional = "Optional 2"
        };

        #endregion

        Assert.True(filter1.Any());
        Assert.False(filter2.Any());
    }

    [Fact]
    public void All_Test()
    {
        #region Prepare data

        var filter1 = new TestFilter1
        {
            Id = "1",
            Name = "Test 1",
            Prop = "Prop 1",
            Date = DateTime.Now,
            Items1 = new List<string> { "1", "2", "3" },
            Items2 = new List<string> { "4", "5", "6" },
        };

        var filter2 = new TestFilter1
        {
            Id = "1",
            Name = "Test 1",
            Prop = "Prop 1",
            Items1 = new List<string> { "1", "2", "3" },
            Items2 = new List<string> { "4", "5", "6" },
        };

        var filter3 = new TestFilter1
        {
            Id = "1",
            Name = "Test 1",
            Prop = "Prop 1",
            Items1 = new List<string> { "1", "2", "3" },
        };

        #endregion

        Assert.True(filter1.All());
        Assert.False(filter2.All());
        Assert.False(filter3.All());
    }

    [Fact]
    public void GetQueryExpression_StringEquals_Test()
    {
        #region Prepare data

        var filter = new TestFilter1
        {
            Id = "1",
            Name = "Test 1",
            Prop = "Prop 1",
            Items1 = new List<string> { "1", "2" },
            Items2 = new List<string> { "4", "5" },
        };

        var models = new List<TestModel1>
        {
            new TestModel1
            {
                Id = "1",
                Name = "Test 1",
                Prop = "Prop 1",
                Item1 = "1",
                Item2 = "4",
            },
            new TestModel1
            {
                Id = "2",
                Name = "Test 2",
                Prop = "Prop 2",
                Item1 = "7",
                Item2 = "10",
            },
            new TestModel1
            {
                Id = "3",
                Name = "Test 3",
                Prop = "Prop 3",
                Item1 = "13",
                Item2 = "16",
            }
        };

        #endregion

        var expected = models.First(i => i.Id == "1");
        var expression = filter.GetQueryExpression();
        var result = models.AsQueryable().FirstOrDefault(expression);
        
        Assert.Equal(expected, result);
    }

    [Fact]
    public void GetQueryExpression_List_Test()
    {
        #region Prepare data

        var filter = new TestFilter2
        {
            Id = "1",
            Name = "Test 1",
            Prop = "Prop 1",
            Items1 = new List<string> { "1", "2" },
            Items2 = new List<string> { "4", "5" },
        };

        var models = new List<TestModel2>
        {
            new TestModel2
            {
                Id = "1",
                Name = "Test 1",
                Prop = "Prop 1",
                Items1 = new List<string> { "1", "2", "3" },
                Items2 = new List<string> { "4", "5", "6" },
            },
            new TestModel2
            {
                Id = "2",
                Name = "Test 2",
                Prop = "Prop 2",
                Items1 = new List<string> { "7", "8", "9" },
                Items2 = new List<string> { "10", "11", "12" },
            },
            new TestModel2
            {
                Id = "3",
                Name = "Test 3",
                Prop = "Prop 3",
                Items1 = new List<string> { "13", "14", "15" },
                Items2 = new List<string> { "16", "17", "18" },
            }
        };

        #endregion

        var expected = models.First(i => i.Id == "1");
        var expression = filter.GetQueryExpression();
        var result = models.AsQueryable().FirstOrDefault(expression);

        Assert.Equal(expected, result);
    }

    [Fact]
    public void GetQueryExpression_StringCaseInsensitive_Test()
    {
        #region Prepare data

        var filter = new TestFilter1CaseInsensitive
        {
            Id = "1",
            Name = "Test 1",
            Prop = "Prop 1",
            Items1 = new List<string> { "One", "Two" },
            Items2 = new List<string> { "Four", "Five" },
        };

        var models = new List<TestModel1>
        {
            new TestModel1
            {
                Id = "1",
                Name = "Test 1",
                Prop = "prop 1",
                Item1 = "one",
                Item2 = "four"
            },
            new TestModel1
            {
                Id = "2",
                Name = "Test 2",
                Prop = "prop 2",
                Item1 = "Two",
                Item2 = "Five"
            },
            new TestModel1
            {
                Id = "3",
                Name = "Test 3",
                Prop = "prop 3",
                Item1 = "Three",
                Item2 = "Six"
            }
        };

        #endregion

        var expected = models.First(i => i.Id == "1");
        var expression = filter.GetQueryExpression();
        var result = models.AsQueryable().FirstOrDefault(expression);

        Assert.Equal(expected, result);
    }

    [Fact]
    public void GetQueryExpression_ListCaseInsensitive_Test()
    {
        #region Prepare data

        var filter = new TestFilter2CaseInsensitive
        {
            Id = "1",
            Name = "Test 1",
            Prop = "Prop 1",
            Items1 = new List<string> { "One", "Two" },
            Items2 = new List<string> { "Four", "Five" },
        };

        var models = new List<TestModel2>
        {
            new TestModel2
            {
                Id = "1",
                Name = "Test 1",
                Prop = "Prop 1",
                Items1 = new List<string> { "one", "two", "three" },
                Items2 = new List<string> { "four", "five", "six" },
            },
            new TestModel2
            {
                Id = "2",
                Name = "Test 2",
                Prop = "Prop 2",
                Items1 = new List<string> { "seven", "eight", "nine" },
                Items2 = new List<string> { "ten", "eleven", "twelve" },
            },
            new TestModel2
            {
                Id = "3",
                Name = "Test 3",
                Prop = "Prop 3",
                Items1 = new List<string> { "13", "14", "15" },
                Items2 = new List<string> { "16", "17", "18" },
            }
        };

        #endregion

        var expected = models.First(i => i.Id == "1");
        var expression = filter.GetQueryExpression();
        var result = models.AsQueryable().FirstOrDefault(expression);

        Assert.Equal(expected, result);
    }

    [Fact]
    public void GetQueryExpression_StringEmpty_Test()
    {
        #region Prepare data

        var filter = new TestFilterEmpty
        {
            Id = "1",
            Name = "Test 1",
            Prop = "",
            Items1 = new List<string> { "One", "Two" }
        };

        var models = new List<TestModel1>
        {
            new TestModel1
            {
                Id = "1",
                Name = "Test 1",
                Prop = "Prop 1",
                Item1 = "One",
            },
            new TestModel1
            {
                Id = "2",
                Name = "Test 2",
                Prop = "Prop 2",
                Item1 = "Two",
            },
            new TestModel1
            {
                Id = "3",
                Name = "Test 3",
                Prop = "Prop 3",
                Item1 = "Three",
                Item2 = "Six"
            }
        };

        #endregion

        var expression = filter.GetQueryExpression();
        var result = models.AsQueryable().FirstOrDefault(expression);

        Assert.Null(result);
    }

    [Fact]
    public void GetQueryExpression_ListEmpty_Test()
    {
        #region Prepare data

        var filter = new TestFilterEmpty
        {
            Id = "1",
            Name = "Test 1",
            Prop = "Prop 1",
            Items1 = new List<string>(),
        };

        var models = new List<TestModel1>
        {
            new TestModel1
            {
                Id = "1",
                Name = "Test 1",
                Prop = "Prop 1",
                Item1 = "One",
            },
            new TestModel1
            {
                Id = "2",
                Name = "Test 2",
                Prop = "Prop 2",
                Item1 = "Two",
            },
            new TestModel1
            {
                Id = "3",
                Name = "Test 3",
                Prop = "Prop 3",
                Item1 = "Three",
                Item2 = "Six"
            }
        };

        #endregion

        var expression = filter.GetQueryExpression();
        var result = models.AsQueryable().FirstOrDefault(expression);

        Assert.Null(result);
    }

    [Fact]
    public void GetQueryExpression_Missing_Fail_Test()
    {
        #region Prepare data

        var filter = new TestFilter3
        {
            Id = "1",
            Name = "Test 1",
            Prop = "Prop 1",
            Items1 = new List<string> { "1", "2" },
            Items2 = new List<string> { "4", "5" },
        };

        var models = new List<TestModel3>
        {
            new TestModel3
            {
                Id = "1",
                Name = "Test 1",
                Prop = "Prop 1",
                Items1 = new List<string> { "1", "2", "3" },
            },
            new TestModel3
            {
                Id = "2",
                Name = "Test 2",
                Prop = "Prop 2",
                Items1 = new List<string> { "7", "8", "9" },
            },
            new TestModel3
            {
                Id = "3",
                Name = "Test 3",
                Prop = "Prop 3",
                Items1 = new List<string> { "13", "14", "15" },
            }
        };

        #endregion

        Assert.Throws<FilterException>(() => filter.GetQueryExpression());
    }

    [Fact]
    public void GetQueryExpression_Missing_Success_Test()
    {
        #region Prepare data

        var filter = new TestFilter3
        {
            Id = "1",
            Name = "Test 1",
            Prop = "Prop 1",
            Items1 = new List<string> { "1", "2" },
            Items2 = new List<string> { "4", "5" },
        };

        var models = new List<TestModel3>
        {
            new TestModel3
            {
                Id = "1",
                Name = "Test 1",
                Prop = "Prop 1",
                Items1 = new List<string> { "1", "2", "3" },
            },
            new TestModel3
            {
                Id = "2",
                Name = "Test 2",
                Prop = "Prop 2",
                Items1 = new List<string> { "7", "8", "9" },
            },
            new TestModel3
            {
                Id = "3",
                Name = "Test 3",
                Prop = "Prop 3",
                Items1 = new List<string> { "13", "14", "15" },
            }
        };

        #endregion

        var expected = models.First(i => i.Id == "1");
        var expression = filter.GetQueryExpression(new FilterOptions { Optimistic = true });
        var result = models.AsQueryable().FirstOrDefault(expression);
    }

    [Fact]
    public void GetQueryExpression_StringContains_Test()
    {
        #region Prepare data

        var filter = new TestFilter4
        {
            Id = "1",
            Name = "Test 1",
            Prop = "Prop 1",
            Items1 = new List<string> { "1", "2" },
            Items2 = new List<string> { "4", "5" },
        };

        var models = new List<TestModel1>
        {
            new TestModel1
            {
                Id = "1",
                Name = "Test 1",
                Prop = "Prop 11",
                Item1 = "1",
                Item2 = "4",
            },
            new TestModel1
            {
                Id = "2",
                Name = "Test 2",
                Prop = "Prop 21",
                Item1 = "7",
                Item2 = "10",
            },
            new TestModel1
            {
                Id = "3",
                Name = "Test 3",
                Prop = "Prop 31",
                Item1 = "13",
                Item2 = "16",
            }
        };

        #endregion

        var expected = models.First(i => i.Id == "1");
        var expression = filter.GetQueryExpression();
        var result = models.AsQueryable().FirstOrDefault(expression);

        Assert.Equal(expected, result);
    }

    [Fact]
    public void GetQueryExpression_DatesEqual_Test()
    {
        #region Prepare data

        var filter = new TestFilterDate1
        {
            Id = "1",
            Name = "Test 1",
            Date = DateTime.Now.Date,
        };

        var models = new List<TestModelDate>
        {
            new TestModelDate
            {
                Id = "1",
                Name = "Test 1",
                Date = DateTime.Now.Date
            },
            new TestModelDate
            {
                Id = "2",
                Name = "Test 2",
                Date = DateTime.Now.AddDays(1).Date
            },
            new TestModelDate
            {
                Id = "3",
                Name = "Test 3",
                Date = DateTime.Now.AddDays(1).Date
            }
        };

        #endregion

        var expected = models.First(i => i.Id == "1");
        var expression = filter.GetQueryExpression();
        var result = models.AsQueryable().FirstOrDefault(expression);

        Assert.Equal(expected, result);
    }

    [Fact]
    public void GetQueryExpression_DatesNotEqual_Test()
    {
        #region Prepare data

        var filter = new TestFilterDate2
        {
            Id = "1",
            Name = "Test 1",
            Date = DateTime.Now.Date,
        };

        var models = new List<TestModelDate>
        {
            new TestModelDate
            {
                Id = "1",
                Name = "Test 1",
                Date = DateTime.Now.AddDays(1).Date
            },
            new TestModelDate
            {
                Id = "2",
                Name = "Test 2",
                Date = DateTime.Now.Date
            },
            new TestModelDate
            {
                Id = "3",
                Name = "Test 3",
                Date = DateTime.Now.Date
            }
        };

        #endregion

        var expected = models.First(i => i.Id == "1");
        var expression = filter.GetQueryExpression();
        var result = models.AsQueryable().FirstOrDefault(expression);

        Assert.Equal(expected, result);
    }

    [Fact]
    public void GetQueryExpression_DatesBetween1_Test()
    {
        #region Prepare data

        var filter = new TestFilterDate3
        {
            Id = "1",
            Name = "Test 1",
            StartDate = DateTime.ParseExact("01/01/2022", "d", CultureInfo.InvariantCulture),
            EndDate = DateTime.ParseExact("01/20/2022", "d", CultureInfo.InvariantCulture),

        };

        var models = new List<TestModelDate>
        {
            new TestModelDate
            {
                Id = "1",
                Name = "Test 1",
                Date = DateTime.ParseExact("01/10/2022", "d", CultureInfo.InvariantCulture)
            },
            new TestModelDate
            {
                Id = "2",
                Name = "Test 2",
                Date = DateTime.ParseExact("01/01/2022", "d", CultureInfo.InvariantCulture)
            },
            new TestModelDate
            {
                Id = "3",
                Name = "Test 3",
                Date = DateTime.ParseExact("01/20/2022", "d", CultureInfo.InvariantCulture)
            }
        };

        #endregion

        var expected = models.First(i => i.Id == "1");
        var expression = filter.GetQueryExpression();
        var result = models.AsQueryable().FirstOrDefault(expression);

        Assert.Equal(expected, result);
    }

    [Fact]
    public void GetQueryExpression_DatesBetween2_Test()
    {
        #region Prepare data

        var filter = new TestFilterDate4
        {
            Id = "1",
            Name = "Test 1",
            StartDate = DateTime.ParseExact("01/01/2022", "d", CultureInfo.InvariantCulture),
            EndDate = DateTime.ParseExact("01/20/2022", "d", CultureInfo.InvariantCulture),

        };

        var models = new List<TestModelDate>
        {
            new TestModelDate
            {
                Id = "1",
                Name = "Test 1",
                Date = DateTime.ParseExact("01/01/2022", "d", CultureInfo.InvariantCulture)
            },
            new TestModelDate
            {
                Id = "2",
                Name = "Test 2",
                Date = DateTime.ParseExact("12/31/2021", "d", CultureInfo.InvariantCulture)
            },
            new TestModelDate
            {
                Id = "3",
                Name = "Test 3",
                Date = DateTime.ParseExact("01/21/2022", "d", CultureInfo.InvariantCulture)
            }
        };

        #endregion

        var expected = models.First(i => i.Id == "1");
        var expression = filter.GetQueryExpression();
        var result = models.AsQueryable().FirstOrDefault(expression);

        Assert.Equal(expected, result);
    }

    [Fact]
    public void GetQueryExpression_DatesNullableEqual_Test()
    {
        #region Prepare data

        var filter = new TestFilterDateNullable1
        {
            Id = "1",
            Name = "Test 1",
            Date = DateTime.Now.Date,
        };

        var models = new List<TestModelDateNullable>
        {
            new TestModelDateNullable
            {
                Id = "1",
                Name = "Test 1",
                Date = DateTime.Now.Date
            },
            new TestModelDateNullable
            {
                Id = "2",
                Name = "Test 2",
                Date = DateTime.Now.AddDays(1).Date
            },
            new TestModelDateNullable
            {
                Id = "3",
                Name = "Test 3",
                Date = DateTime.Now.AddDays(1).Date
            }
        };

        #endregion

        var expected = models.First(i => i.Id == "1");
        var expression = filter.GetQueryExpression();
        var result = models.AsQueryable().FirstOrDefault(expression);

        Assert.Equal(expected, result);
    }

    [Fact]
    public void GetQueryExpression_DatesNullableNull_Test()
    {
        #region Prepare data

        var filter = new TestFilterDateNullable1
        {
            Id = "1",
            Name = "Test 1",
            Date = DateTime.Now.Date,
        };

        var models = new List<TestModelDateNullable>
        {
            new TestModelDateNullable
            {
                Id = "1",
                Name = "Test 1",
                Date = null
            },
            new TestModelDateNullable
            {
                Id = "2",
                Name = "Test 2",
                Date = DateTime.Now.AddDays(1).Date
            },
            new TestModelDateNullable
            {
                Id = "3",
                Name = "Test 3",
                Date = DateTime.Now.AddDays(1).Date
            }
        };

        #endregion

        var expression = filter.GetQueryExpression();
        var result = models.AsQueryable().Where(expression);

        Assert.Empty(result);
    }

    [Fact]
    public void GetQueryExpression_DefaultLogic_Test()
    {
        #region Prepare data

        var filter = new TestFilterDefaultLogic
        {
            Name = "Test 1",
            Item1 = "1",
            Item2 = "2",
        };

        var models = new List<TestModel1>
        {
            new TestModel1
            {
                Id = "1",
                Name = "Test 1",
                Item1 = "1",
                Item2 = "2",
            },
            new TestModel1
            {
                Id = "2",
                Name = "Test 1",
                Item1 = "2",
                Item2 = "3",
            },
            new TestModel1
            {
                Id = "3",
                Name = "Test 1",
                Item1 = "1",
                Item2 = "3",
            }
        };

        #endregion

        var expected = models.First(i => i.Id == "1");
        var expression = filter.GetQueryExpression();
        var result = models.AsQueryable().Single(expression);

        Assert.Equal(expected, result);
    }

    [Fact]
    public void GetQueryExpression_AndLogic_Test()
    {
        #region Prepare data

        var filter = new TestFilterAndLogic
        {
            Name = "Test 1",
            Item1 = "1",
            Item2 = "2",
        };

        var models = new List<TestModel1>
        {
            new TestModel1
            {
                Id = "1",
                Name = "Test 1",
                Item1 = "1",
                Item2 = "2",
            },
            new TestModel1
            {
                Id = "2",
                Name = "Test 1",
                Item1 = "2",
                Item2 = "3",
            },
            new TestModel1
            {
                Id = "3",
                Name = "Test 1",
                Item1 = "1",
                Item2 = "3",
            }
        };

        #endregion

        var expected = models.First(i => i.Id == "1");
        var expression = filter.GetQueryExpression();
        var result = models.AsQueryable().Single(expression);

        Assert.Equal(expected, result);
    }

    [Fact]
    public void GetQueryExpression_OrLogic_Test()
    {
        #region Prepare data

        var filter = new TestFilterOrLogic
        {
            Name = "Test 1",
            Item1 = "1",
            Item2 = "2",
        };

        var models = new List<TestModel1>
        {
            new TestModel1
            {
                Id = "1",
                Name = "Test 1",
                Item1 = "1",
                Item2 = "2",
            },
            new TestModel1
            {
                Id = "2",
                Name = "Test 1",
                Item1 = "2",
                Item2 = "3",
            },
            new TestModel1
            {
                Id = "3",
                Name = "Test 1",
                Item1 = "1",
                Item2 = "3",
            },
            new TestModel1
            {
                Id = "4",
                Name = "Test 4",
                Item1 = "3",
                Item2 = "2",
            }
        };

        #endregion

        var expected = models.Where(i => i.Id == "1" || i.Id == "3" || i.Id == "4").ToList();
        var expression = filter.GetQueryExpression();
        var result = models.AsQueryable().Where(expression).ToList();

        Assert.Equal(expected, result);
    }
}


#region Test classes

#region Test Filters

public class TestFilter1 : Filter<TestModel1>
{
    [FilterMember]
    public string Id { get; set; }

    [FilterMember]
    public string Name { get; set; }

    [FilterMember]
    public string Prop { get; set; }

    [FilterMember]
    public DateTime? Date { get; set; }

    [FilterMember(name:"Item1")]
    public List<string> Items1 { get; set; }

    [FilterMember(name: "Item2")]
    public List<string> Items2 { get; set; }

    public string Optional { get; set; }
}

public class TestFilter2 : Filter<TestModel2>
{
    [FilterMember]
    public string Id { get; set; }

    [FilterMember]
    public string Name { get; set; }

    [FilterMember]
    public string Prop { get; set; }

    [FilterMember]
    public List<string> Items1 { get; set; }

    [FilterMember]
    public List<string> Items2 { get; set; }

    public string Optional { get; set; }
}

public class TestFilter3 : Filter<TestModel3>
{
    [FilterMember]
    public string Id { get; set; }

    [FilterMember]
    public string Name { get; set; }

    [FilterMember]
    public string Prop { get; set; }

    [FilterMember]
    public List<string> Items1 { get; set; }

    [FilterMember]
    public List<string> Items2 { get; set; }

    public string Optional { get; set; }
}

public class TestFilter4 : Filter<TestModel1>
{
    [FilterMember]
    public string Id { get; set; }

    [FilterMember]
    public string Name { get; set; }

    [FilterMember(comparisonMethod: StringComparisonMethod.Contains)]
    public string Prop { get; set; }

    [FilterMember(name: "Item1")]
    public List<string> Items1 { get; set; }

    [FilterMember(name: "Item2")]
    public List<string> Items2 { get; set; }

    public string Optional { get; set; }
}

public class TestFilter1CaseInsensitive : Filter<TestModel1>
{
    [FilterMember]
    public string Id { get; set; }

    [FilterMember]
    public string Name { get; set; }

    [FilterMember(comparisonType: StringComparison.InvariantCultureIgnoreCase)]
    public string Prop { get; set; }

    [FilterMember(name: "Item1", comparisonType: StringComparison.InvariantCultureIgnoreCase)]
    public List<string> Items1 { get; set; }

    [FilterMember(name: "Item2", comparisonType: StringComparison.InvariantCultureIgnoreCase)]
    public List<string> Items2 { get; set; }

    public string Optional { get; set; }
}

public class TestFilter2CaseInsensitive : Filter<TestModel2>
{
    [FilterMember]
    public string Id { get; set; }

    [FilterMember]
    public string Name { get; set; }

    [FilterMember]
    public string Prop { get; set; }

    [FilterMember(comparisonType: StringComparison.InvariantCultureIgnoreCase)]
    public List<string> Items1 { get; set; }

    [FilterMember(comparisonType: StringComparison.InvariantCultureIgnoreCase)]
    public List<string> Items2 { get; set; }

    public string Optional { get; set; }
}

public class TestFilterEmpty : Filter<TestModel1>
{
    [FilterMember]
    public string Id { get; set; }

    [FilterMember]
    public string Name { get; set; }

    [FilterMember(ignoreIfEmpty: false)]
    public string Prop { get; set; }

    [FilterMember(name: "Item1", ignoreIfEmpty: false)]
    public List<string> Items1 { get; set; }
}

public class TestFilterDate1 : Filter<TestModelDate>
{
    [FilterMember]
    public string Id { get; set; }

    [FilterMember]
    public string Name { get; set; }

    [FilterMember(comparisonOperation: ComparisonOperation.Equality)]
    public DateTime? Date { get; set; }
}

public class TestFilterDate2 : Filter<TestModelDate>
{
    [FilterMember]
    public string Id { get; set; }

    [FilterMember]
    public string Name { get; set; }

    [FilterMember(comparisonOperation: ComparisonOperation.Inequality)]
    public DateTime? Date { get; set; }
}

public class TestFilterDate3 : Filter<TestModelDate>
{
    [FilterMember]
    public string Id { get; set; }

    [FilterMember]
    public string Name { get; set; }

    [FilterMember(name: "Date", comparisonOperation: ComparisonOperation.LessThan)]
    public DateTime? StartDate { get; set; }

    [FilterMember(name: "Date", comparisonOperation: ComparisonOperation.GreaterThan)]
    public DateTime? EndDate { get; set; }
}

public class TestFilterDate4 : Filter<TestModelDate>
{
    [FilterMember]
    public string Id { get; set; }

    [FilterMember]
    public string Name { get; set; }

    [FilterMember(name: "Date", comparisonOperation: ComparisonOperation.LessThanOrEqual)]
    public DateTime? StartDate { get; set; }

    [FilterMember(name: "Date", comparisonOperation: ComparisonOperation.GreaterThanOrEqual)]
    public DateTime? EndDate { get; set; }
}

public class TestFilterDateNullable1 : Filter<TestModelDateNullable>
{
    [FilterMember]
    public string Id { get; set; }

    [FilterMember]
    public string Name { get; set; }

    [FilterMember(comparisonOperation: ComparisonOperation.Equality)]
    public DateTime? Date { get; set; }
}

public class TestFilterDefaultLogic : Filter<TestModel1>
{
    [FilterMember]
    public string Id { get; set; }

    [FilterMember]
    public string Name { get; set; }

    [FilterMember]
    public string Item1 { get; set; }

    [FilterMember]
    public string Item2 { get; set; }
}

public class TestFilterAndLogic : Filter<TestModel1>
{
    [FilterMember]
    public string Id { get; set; }

    [FilterMember]
    public string Name { get; set; }

    [FilterMember]
    public string Item1 { get; set; }

    [FilterMember(logicalOperation: LogicalOperation.And)]
    public string Item2 { get; set; }
}

public class TestFilterOrLogic : Filter<TestModel1>
{
    [FilterMember]
    public string Id { get; set; }

    [FilterMember]
    public string Name { get; set; }

    [FilterMember]
    public string Item1 { get; set; }

    [FilterMember(logicalOperation: LogicalOperation.Or)]
    public string Item2 { get; set; }
}

#endregion Test Filters

#region Test Models

public class TestModel1
{
    public string Id { get; set; }
    public string Name { get; set; }
    public string Prop { get; set; }
    public DateTime? Date { get; set; }
    public string Item1 { get; set; }
    public string Item2 { get; set; }
}

public class TestModel2
{
    public string Id { get; set; }
    public string Name { get; set; }
    public string Prop { get; set; }
    public List<string> Items1 { get; set; }
    public List<string> Items2 { get; set; }
}

public class TestModel3
{
    public string Id { get; set; }
    public string Name { get; set; }
    public string Prop { get; set; }
    public List<string> Items1 { get; set; }
}

public class TestModelDate
{
    public string Id { get; set; }
    public string Name { get; set; }
    public DateTime Date { get; set; }
}

public class TestModelDateNullable
{
    public string Id { get; set; }
    public string Name { get; set; }
    public DateTime? Date { get; set; }
}

#endregion Test Models

#endregion