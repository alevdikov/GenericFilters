using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using GenericFilters.SqlQueryBuilder.Dialects;

namespace GenericFilters.SqlQueryBuilder;

/// <summary>
/// Walks a filter expression looking for single-level navigation-property access
/// (e.g. x.Category.Name) and resolves the JOIN needed to reach it, using the same
/// {NavType}{KeyPropertyName} foreign-key convention as the rest of the library.
/// </summary>
internal sealed class JoinResolver : ExpressionVisitor
{
    private readonly Type _rootType;
    private readonly ISqlDialect _dialect;
    private readonly HashSet<string> _usedAliases;
    private readonly Dictionary<PropertyInfo, JoinInfo> _joins = new();

    public JoinResolver(Type rootType, ISqlDialect dialect, string rootAlias)
    {
        _rootType = rootType;
        _dialect = dialect;
        _usedAliases = new HashSet<string> { rootAlias };
    }

    public IReadOnlyDictionary<PropertyInfo, JoinInfo> Resolve(Expression expression)
    {
        Visit(expression);
        return _joins;
    }

    protected override Expression VisitMember(MemberExpression node)
    {
        if (node.Expression is MemberExpression { Expression: ParameterExpression } navMember &&
            navMember.Member is PropertyInfo navProp &&
            navProp.DeclaringType == _rootType &&
            !_joins.ContainsKey(navProp))
        {
            TryRegisterJoin(navProp);
        }

        return base.VisitMember(node);
    }

    private void TryRegisterJoin(PropertyInfo navProp)
    {
        var navType = navProp.PropertyType;
        var keyProp = navType.GetProperties()
            .FirstOrDefault(p => p.GetCustomAttribute<KeyAttribute>() != null);
        if (keyProp == null) return;

        var fkProp = FindForeignKeyProperty(navProp, keyProp);
        if (fkProp == null) return;

        _joins[navProp] = new JoinInfo
        {
            NavProperty = navProp,
            ForeignKeyProperty = fkProp,
            KeyProperty = keyProp,
            Alias = UniqueAlias(_dialect.GetTableAlias(navType))
        };
    }

    /// <summary>
    /// Mirrors EF Core's own FK-discovery convention: {NavPropertyName}{KeyPropertyName},
    /// then {NavPropertyName}Id, then a property matching the principal key name directly.
    /// </summary>
    private PropertyInfo FindForeignKeyProperty(PropertyInfo navProp, PropertyInfo keyProp)
    {
        var candidates = new[]
        {
            navProp.Name + keyProp.Name,
            navProp.Name + "Id",
            keyProp.Name
        };

        foreach (var candidate in candidates)
        {
            var property = _rootType.GetProperty(candidate);
            if (property != null) return property;
        }

        return null;
    }

    private string UniqueAlias(string alias)
    {
        var candidate = alias;
        var suffix = 1;
        while (!_usedAliases.Add(candidate))
            candidate = alias + ++suffix;
        return candidate;
    }
}