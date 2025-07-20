---

# 🔍 GenericFilters [![NuGet](https://img.shields.io/nuget/v/genericFilters.svg)](https://www.nuget.org/packages/genericFilters/)

A powerful and extensible filtering framework for C# applications. `GenericFilters` enables dynamic, attribute-driven filtering logic for LINQ queries, in-memory collections, and Cosmos DB SDK queries.

---

## 🔧 Installation

### .NET CLI
```bash
dotnet add package GenericFilters
```

### Package Manager

Install-Package GenericFilters

---

## 📦 Features

- ✅ Attribute-based filtering with `FilterMemberAttribute`
- 🔄 Supports string, list, `DateTime` and numeric comparisons
- 🧠 Logical operations (`AND` / `OR`) between filters
- 🧰 Customizable filter behavior with `FilterOptions`
- 🧪 Built-in validation and error handling
- ⚙️ Expression tree generation for LINQ queries

---

## 📁 Components

### 1. `Filter`

An abstract base class that:
- Validates filter properties at runtime
- Generates LINQ expressions dynamically
- Supports pagination (`StartingIndex`, `PageSize`)
- Provides utility methods: `Any()`, `All()`, `GetQueryExpression()`

### 2. `FilterMemberAttribute`

Decorates filter properties to define:
- Target model property (`Name`)
- String comparison method (`Equals`, `Contains`)
- Case sensitivity
- Dates and numbers comparison operations
- Logical grouping (`And`, `Or`)
- Inclusion/exclusion in query generation

### 3. `FilterOptions`

Controls runtime behavior:
- `Optimistic`: If `true`, ignores missing model properties instead of throwing exceptions

---

## 🚀 Getting Started

### 1. Define a Model

```csharp
public record Product(string Name, List<string> Tags, 
    double UnitPrice, DateTime? CreatedAt);
```

### 2. Define a Filter

```csharp
using GenericFilters;

public class ProductFilter : Filter<Product>
{
    [FilterMember]
    public string Name { get; init; }

    [FilterMember(stringComparisonMethod: StringComparisonMethod.Contains, stringComparisonIgnoreCase: true)]
    public List<string> Tags { get; init; }
    
    [FilterMember("UnitPrice", comparisonOperation: ComparisonOperation.GreaterThanOrEqual)]
    public double? PriceFrom { get; init; }
    
    [FilterMember("UnitPrice", comparisonOperation: ComparisonOperation.LessThan)]
    public double? PriceTo { get; init; }

    [FilterMember("CreatedAt", comparisonOperation: ComparisonOperation.GreaterThanOrEqual)]
    public DateTime? StartDate { get; init; }

    [FilterMember("CreatedAt", comparisonOperation: ComparisonOperation.LessThanOrEqual)]
    public DateTime? EndDate { get; init; }
}
```

### 3. Apply the Filter

```csharp
using GenericFilters.Extensions;

var products = new List<Product>
{
    new Product("book", [ "education", "programming", "software" ], 
        58.90, new DateTime(2025, 2, 12)),
    new Product("phone", [ "android", "electronics" ],
        770.00, new DateTime(2023, 6, 8)),
    new Product("laptop", [ "linux", "electronics", "entertaiment" ], 
        3999.99, new DateTime(2024, 7, 22)),
};

var filter = new ProductFilter
{
    Name = "book", 
    Tags = [ "education", "programing1" ], 
    PriceFrom = 40.00,
    PriceTo = 100.00,
    StartDate = new DateTime(2025, 2, 1), 
    EndDate = new DateTime(2025, 3, 1)
};

var filteredProducts = products.FilterBy(filter).ToList();
```

### 3.1. Same scenario using GetQueryExpression() method

```csharp
var expression = filter.GetQueryExpression();

var filteredProducts = products.AsQueryable()
    .Where(expression)
    .ToList();
```

▶️ [Run this code on .NET Fiddle][dotnet1]

[dotnet1]: https://dotnetfiddle.net/id3pVo

---

## ⚙️ Advanced Options

### Optimistic Filtering

```csharp
var options = new FilterOptions { Optimistic = true };
var expression = filter.GetQueryExpression(options);
```

This allows filters to skip missing model properties without throwing exceptions.

### IgnoreInQueryExpression

When IgnoreInQueryExpression is set to `true` in the FilterMember attribute, that property is skipped when we build 
Filter expression   

```csharp
[FilterMember(ignoreInQueryExpression: true)]
public List<string> Items { get; init; }
```
You can use that parameter in cases when you need to implement any custom filtering logic.
More details about custom filtering is provided under section related to GetQueryExpressionExt method

It is the same when we don't provide any attribute for that property at all, but the difference is,
If we provide FilterMember, that property will be taken into consideration when we call 
getHashCode(), Any() or All() methods of the Filter class. 

### GetQueryExpressionExt method

In some cases in our model or our filter we may have some complex properties not handled by the Filter 
out of the box.  
Or probably we need to provide some specific logic with extra conditions. 

In that case we can apply the following approach:
- Apply FilterMember attributes for all strings, numeric etc. properties as usually. 
- Mark all properties with custom logic we are going to provide as IgnoreInQueryExpression
- Override GetQueryExpressionExt and add all custom logic using LinqKit or build Linq Expression in any another way.

When we call either GetQueryExpression or FilterBy, that custom Linq Expression will be added to the end of 
Expression generated for our 'standard' filter behind the scene.

Here is an example of using that approach to filter by ProductItem type property:

### 1. Define a Model

```csharp
public class Product
{
    public string Name { get; init; }
    public List<string> Tags { get; init; }
    public double UnitPrice { get; init; } 
    public List<ProductItem> Items { get; init; }
    public DateTime CreatedAt { get; init; }
};

public record ProductItem (string SKU, int Quantity);
```

### 2. Define a Filter

```csharp
using System.Linq.Expressions;
using GenericFilters;
using LinqKit;

public class ProductFilter : Filter<Product>
{
    [FilterMember]
    public string Name { get; init; }

    [FilterMember(stringComparisonMethod: StringComparisonMethod.Contains, stringComparisonIgnoreCase: true)]
    public List<string> Tags { get; init; }
    
    [FilterMember(ignoreInQueryExpression: true)]
    public List<string> Items { get; init; }
    
    [FilterMember("UnitPrice", comparisonOperation: ComparisonOperation.GreaterThanOrEqual)]
    public double? PriceFrom { get; init; }
    
    [FilterMember("UnitPrice", comparisonOperation: ComparisonOperation.LessThan)]
    public double? PriceTo { get; init; }

    [FilterMember("CreatedAt", comparisonOperation: ComparisonOperation.GreaterThanOrEqual)]
    public DateTime? StartDate { get; init; }

    [FilterMember("CreatedAt", comparisonOperation: ComparisonOperation.LessThanOrEqual)]
    public DateTime? EndDate { get; init; }
    
    protected override Expression<Func<Product, bool>> GetQueryExpressionExt(FilterOptions filterOptions)
    {
        var predicate = PredicateBuilder.New<Product>();
        
        // Build custom behaviour for Items using LinqKit
        predicate.And(i => Items
            .Any(t => i.Items.Any(i => i.SKU == t)));
        
        return predicate;
    }
}
```

### 3. Apply the Filter

```csharp
using GenericFilters.Extensions;

var products = new List<Product>
{
    new Product
    {
        Name = "External Storage Bundle",
        Tags = new List<string> { "storage", "bundle", "external" },
        UnitPrice = 129.99,
        CreatedAt = new DateTime(2025, 2, 1),
        Items = new List<ProductItem>
        {
            new ProductItem("HD-1001", 25),
            new ProductItem("USB-64GB", 100),
            new ProductItem("SD-128GB", 50)
        }
    },
    new Product
    {
        Name = "Portable Backup Kit",
        Tags = new List<string> { "backup", "portable", "data", "storage" },
        UnitPrice = 89.99,
        CreatedAt = new DateTime(2025, 2, 1),
        Items = new List<ProductItem>
        {
            new ProductItem("HD-1001", 20),
            new ProductItem("CASE-01", 30)
        }
    },
    new Product
    {
        Name = "Hard Drive",
        Tags = new List<string> { "storage", "hard drive" },
        UnitPrice = 59.99,
        CreatedAt = new DateTime(2025, 2, 1),
        Items = new List<ProductItem>
        {
            new ProductItem("HD-2002", 40)
        }
    }
};

var filter = new ProductFilter
{
    Tags = [ "data", "storage" ], 
    Items = ["HD-1001" ],
    PriceFrom = 80.00,
    PriceTo = 150.00,
    StartDate = new DateTime(2025, 1, 1), 
};

var filteredProducts = products.AsQueryable()
    .FilterBy(filter)
    .ToList();
```

▶️ [Run this code on .NET Fiddle][dotnet2]

[dotnet2]: https://dotnetfiddle.net/IAPBu2


### Pagination support

There are 2 properties provided in the Filter class in order to support pagination 

```csharp
public int StartIndex { get; set; } = -1;
public int PageSize { get; set; } = -1;
```
Those properties can be set in the Filter and tracked e.g. on UI side. 
When we call FilterBy, internally it will be added Skip() and Take() to our expression. 
If we are using GetQueryExpression, we need to build that logic by ourselves

```csharp
var filteredProducts = products.AsQueryable()
    .Where(filter.GetQueryExpression())
    .Take(filter.StartIndex).Skip(filter.PageSize);
```

---

## 🧪 Validation & Safety

- Only supports `string`, `List<string>`, `DateTime`, `double` and `int` filter types, with nullables
- No nested properties supported
- Throws `FilterException` for unsupported types or misconfigurations
- Ensures at least one valid filter is defined

---

## 📌 Notes

- Not all features are compatible with `IQueryable` in Entity Framework
- Case-insensitive filtering may impact performance if DB collation is not case-sensitive
- You can override `GetQueryExpression()` for custom logic

---

## 📄 License

MIT License

---
