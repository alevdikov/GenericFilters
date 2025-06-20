---

# 🔍 GenericFilters

A powerful and extensible filtering framework for C# applications. `GenericFilters` enables dynamic, attribute-driven filtering logic for LINQ queries, in-memory collections, and Cosmos DB SDK queries.

---

## 📦 Features

- ✅ Attribute-based filtering with `FilterMemberAttribute`
- 🔄 Supports string, list, and `DateTime` comparisons
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
- Date comparison operations
- Logical grouping (`And`, `Or`)
- Inclusion/exclusion in query generation

### 3. `FilterOptions`

Controls runtime behavior:
- `Optimistic`: If `true`, ignores missing model properties instead of throwing exceptions

---

## 🚀 Getting Started

### 1. Define a Filter

```csharp
public class ProductFilter : Filter<Product>
{
    [FilterMember("Name", StringComparisonMethod.Contains, stringComparisonIgnoreCase: true)]
    public string Name { get; set; }

    [FilterMember("Tags", StringComparisonMethod.Equals)]
    public List<string> Tags { get; set; }

    [FilterMember("CreatedAt", comparisonOperation: ComparisonOperation.GreaterThanOrEqual)]
    public DateTime? CreatedAfter { get; set; }
}
```

### 2. Apply the Filter

```csharp
var filter = new ProductFilter { Name = "book", Tags = new List<string> { "education" } };
var expression = filter.GetQueryExpression();

var filteredProducts = dbContext.Products.AsExpandable().Where(expression).ToList();
```

> Requires LinqKit for `AsExpandable()` support.

---

## ⚙️ Advanced Options

### Optimistic Filtering

```csharp
var options = new FilterOptions { Optimistic = true };
var expression = filter.GetQueryExpression(options);
```

This allows filters to skip missing model properties without throwing exceptions.

---

## 🧪 Validation & Safety

- Only supports `string`, `List<string>`, and `DateTime?` filter types
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
