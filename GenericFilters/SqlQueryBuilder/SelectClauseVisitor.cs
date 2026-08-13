using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Reflection;
using GenericFilters.SqlQueryBuilder.Dialects;

namespace GenericFilters.SqlQueryBuilder;

public class SelectClauseVisitor<TModel> where TModel : class
{
    private readonly ISqlDialect _dialect;
    private readonly string _rootAlias;

    public SelectClauseVisitor(ISqlDialect dialect, string rootAlias)
    {
        _dialect = dialect;
        _rootAlias = rootAlias;
    }

    public string Build(IReadOnlyDictionary<PropertyInfo, JoinInfo> joins)
    {
        var table = _dialect.GetTableName(typeof(TModel));
        var from = $"FROM {_dialect.QuoteIdentifier(table)} AS {_dialect.QuoteIdentifier(_rootAlias)}";

        var select = "SELECT " + string.Join(", ", GetOwnColumns().Select(QualifiedColumn));

        var joinClauses = joins.Values.Select(BuildJoinClause);
        var joinsSql = joins.Count > 0 ? "\n" + string.Join("\n", joinClauses) : "";

        return select + "\n" + from + joinsSql;
    }

    private string QualifiedColumn(PropertyInfo property) =>
        $"{_dialect.QuoteIdentifier(_rootAlias)}.{_dialect.QuoteIdentifier(property.Name)}";

    private string BuildJoinClause(JoinInfo join)
    {
        var navTable = _dialect.GetTableName(join.NavProperty.PropertyType);
        return $"INNER JOIN {_dialect.QuoteIdentifier(navTable)} AS {_dialect.QuoteIdentifier(join.Alias)} " +
               $"ON {_dialect.QuoteIdentifier(_rootAlias)}.{_dialect.QuoteIdentifier(join.ForeignKeyProperty.Name)} " +
               $"= {_dialect.QuoteIdentifier(join.Alias)}.{_dialect.QuoteIdentifier(join.KeyProperty.Name)}";
    }

    /// <summary>
    /// Own scalar columns of TModel, ordered the way EF Core orders them by convention:
    /// the primary key first, then the rest alphabetically.
    /// </summary>
    private static IEnumerable<PropertyInfo> GetOwnColumns()
    {
        var props = typeof(TModel).GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => !IsComplexType(p.PropertyType))
            .ToList();

        var keyProp = props.FirstOrDefault(p => p.GetCustomAttribute<KeyAttribute>() != null);
        var rest = props.Where(p => p != keyProp).OrderBy(p => p.Name, StringComparer.Ordinal);

        return keyProp != null ? new[] { keyProp }.Concat(rest) : rest;
    }

    private static bool IsComplexType(Type type) =>
        type.IsClass && type != typeof(string);
}