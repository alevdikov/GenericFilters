---

# 🔍 GenericFilters

A powerful and extensible filtering framework for C# applications. `GenericFilters` enables dynamic, attribute-driven filtering logic for LINQ queries, in-memory collections, and Cosmos DB SDK queries.

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

### 1. `Filter<TModel>`

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
var expression = filter.GetQueryExpression();

var filteredProducts = products.FilterBy(filter).ToList();
```

### 3.1. Same scenario using GetQueryExpression() method

```csharp
var filteredProducts = products.AsQueryable()
    .Where(expression)
    .Take(filter.StartIndex).Skip(filter.PageSize)
    .ToList();
```

## ⚙️ Advanced Options

### Optimistic Filtering

```csharp
var options = new FilterOptions { Optimistic = true };
var expression = filter.GetQueryExpression(options);
```

This allows filters to skip missing model properties without throwing exceptions.

---

## 🧪 Validation & Safety

- Only supports `string`, `List<string>`, `DateTime?`, `double?` and `int?` filter types
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
