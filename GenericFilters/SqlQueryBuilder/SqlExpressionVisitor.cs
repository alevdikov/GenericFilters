using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using GenericFilters.SqlQueryBuilder.Dialects;

namespace GenericFilters.SqlQueryBuilder;

public class SqlExpressionVisitor : ExpressionVisitor
{
    private readonly StringBuilder _sb = new ();
    private readonly ISqlDialect _dialect;
    private readonly string _rootAlias;
    private readonly IReadOnlyDictionary<PropertyInfo, JoinInfo> _joins;
    private readonly Dictionary<ParameterExpression, string> _parameterAliases = new();
    private readonly HashSet<string> _usedAliases;

    public SqlExpressionVisitor(ISqlDialect dialect, string rootAlias, IReadOnlyDictionary<PropertyInfo, JoinInfo> joins)
    {
        _dialect = dialect;
        _rootAlias = rootAlias;
        _joins = joins;
        _usedAliases = new HashSet<string>(joins.Values.Select(j => j.Alias)) { rootAlias };
    }

    public string Build(Expression expression)
    {
        Visit(expression);
        return "WHERE " + _sb;
    }

    protected override Expression VisitBinary(BinaryExpression node)
    {
        // Drop `x.Nav != null` when Nav is already guaranteed by an INNER JOIN.
        if (node.NodeType == ExpressionType.AndAlso && IsRedundantJoinedNavNullCheck(node.Left))
        {
            Visit(node.Right);
            return node;
        }

        if (node.NodeType is ExpressionType.Equal or ExpressionType.NotEqual &&
            (IsNullConstant(node.Left) || IsNullConstant(node.Right)))
        {
            Visit(IsNullConstant(node.Right) ? node.Left : node.Right);
            _sb.Append(node.NodeType == ExpressionType.Equal ? " IS NULL" : " IS NOT NULL");
            return node;
        }

        // Only OR needs explicit grouping here: AND is the implicit top-level joiner,
        // and plain comparisons never need parens around them.
        var needsParens = node.NodeType == ExpressionType.OrElse;
        if (needsParens) _sb.Append("(");

        Visit(node.Left);
        switch (node.NodeType)
        {
            case ExpressionType.Equal: _sb.Append(" = "); break;
            case ExpressionType.NotEqual: _sb.Append(" <> "); break;
            case ExpressionType.GreaterThan: _sb.Append(" > "); break;
            case ExpressionType.GreaterThanOrEqual: _sb.Append(" >= "); break;
            case ExpressionType.LessThan: _sb.Append(" < "); break;
            case ExpressionType.LessThanOrEqual: _sb.Append(" <= "); break;
            case ExpressionType.AndAlso: _sb.Append(" AND "); break;
            case ExpressionType.OrElse: _sb.Append(" OR "); break;
            default: _sb.Append($" {node.NodeType} "); break;
        }
        Visit(node.Right);

        if (needsParens) _sb.Append(")");
        return node;
    }

    private bool IsRedundantJoinedNavNullCheck(Expression expression) =>
        expression is BinaryExpression { NodeType: ExpressionType.NotEqual } binary &&
        IsNullConstant(binary.Right) &&
        binary.Left is MemberExpression { Expression: ParameterExpression, Member: PropertyInfo navProp } &&
        _joins.ContainsKey(navProp);

    private static bool IsNullConstant(Expression expression) =>
        expression is ConstantExpression { Value: null };

    protected override Expression VisitConstant(ConstantExpression node)
    {
        if (node.Value is IEnumerable enumerable && node.Value is not string)
        {
            _sb.Append("(");
            var first = true;
            foreach (var item in enumerable)
            {
                if (!first) _sb.Append(", ");
                _sb.Append(_dialect.FormatLiteral(item));
                first = false;
            }
            _sb.Append(")");
        }
        else
            _sb.Append(_dialect.FormatLiteral(node.Value));
        return node;
    }

    protected override Expression VisitMember(MemberExpression node)
    {
        // x.Property (table column, root or a subquery item introduced by an Any() EXISTS clause)
        if (node.Expression is ParameterExpression parameter)
        {
            _sb.Append($"{_dialect.QuoteIdentifier(GetAliasFor(parameter))}.{_dialect.QuoteIdentifier(node.Member.Name)}");
            return node;
        }

        // x.Nav.Property (joined table column)
        if (node.Expression is MemberExpression { Expression: ParameterExpression, Member: PropertyInfo navProp } &&
            _joins.TryGetValue(navProp, out var join))
        {
            _sb.Append($"{_dialect.QuoteIdentifier(join.Alias)}.{_dialect.QuoteIdentifier(node.Member.Name)}");
            return node;
        }

        // Captured closure variable
        if (node.Expression is ConstantExpression)
        {
            var value = Expression.Lambda(node).Compile().DynamicInvoke();
            _sb.Append(_dialect.FormatLiteral(value));
            return node;
        }

        _sb.Append(node.Member.Name);
        return node;
    }

    protected override Expression VisitMethodCall(MethodCallExpression node)
    {
        switch (node.Method.Name)
        {
            case "Equals":
                Visit(node.Object);
                _sb.Append(" = ");
                Visit(node.Arguments[0]);
                break;
            case "Contains" when node.Object != null && node.Object.Type != typeof(string) && typeof(IEnumerable).IsAssignableFrom(node.Object.Type):
                // instance collection Contains, e.g. list.Contains(x.Category.Name)
                Visit(node.Arguments[0]);
                _sb.Append(" IN ");
                Visit(node.Object);
                break;
            case "Contains" when node.Object != null:
                VisitStringContains(node);
                break;
            case "Contains":
                // static Enumerable.Contains(collection, item)
                Visit(node.Arguments[1]);
                _sb.Append(" IN ");
                Visit(node.Arguments[0]);
                break;
            case "StartsWith":
            case "EndsWith":
                Visit(node.Object);
                _sb.Append(" LIKE ");
                var value = Expression.Lambda(node.Arguments[0]).Compile().DynamicInvoke();
                _sb.Append(node.Method.Name == "StartsWith" ? $"'{value}%'" : $"'%{value}'");
                break;
            case "ToLower":
                _sb.Append("LOWER(");
                Visit(node.Object);
                _sb.Append(")");
                break;
            case "ToUpper":
                _sb.Append("UPPER(");
                Visit(node.Object);
                _sb.Append(")");
                break;
            case "Any" when node.Arguments.Count == 2 && StripQuotes(node.Arguments[1]) is LambdaExpression:
                VisitAny(node.Arguments[0], (LambdaExpression)StripQuotes(node.Arguments[1]));
                break;
            default:
                _sb.Append($"/* Unsupported method: {node.Method.Name} */");
                break;
        }

        return node;
    }

    /// <summary>
    /// Translates `source.Any(item => predicate)`.
    /// - If `source` is a to-many navigation reached off a tracked parameter (e.g. p.Tags),
    ///   emits a correlated EXISTS subquery joined back via the {ParentType}Id convention.
    /// - Otherwise, if `source` is a compile-time-constant collection (e.g. a captured local
    ///   list) that doesn't depend on any query parameter, it's unrolled into an OR of the
    ///   predicate with the item parameter substituted by each literal value. This lets
    ///   patterns like `tags.Any(t => p.Tags.Any(i => i.Name.Equals(t)))` fall through into
    ///   the EXISTS case above once `t` has been replaced by a concrete value.
    /// </summary>
    private void VisitAny(Expression source, LambdaExpression predicate)
    {
        if (source is MemberExpression { Expression: ParameterExpression } nav &&
            typeof(IEnumerable).IsAssignableFrom(nav.Type) && nav.Type != typeof(string))
        {
            BuildExistsSubquery(nav, predicate);
            return;
        }

        if (!ContainsParameter(source))
        {
            var collection = Expression.Lambda(source).Compile().DynamicInvoke() as IEnumerable;
            var items = collection?.Cast<object>().ToList() ?? new List<object>();

            if (items.Count == 0)
            {
                _sb.Append("(1 = 0)");
                return;
            }

            _sb.Append("(");
            for (var i = 0; i < items.Count; i++)
            {
                if (i > 0) _sb.Append(" OR ");
                var itemType = predicate.Parameters[0].Type;
                var substituted = new ParameterReplacer(predicate.Parameters[0], Expression.Constant(items[i], itemType))
                    .Visit(predicate.Body);
                Visit(substituted);
            }
            _sb.Append(")");
            return;
        }

        _sb.Append("/* Unsupported method: Any */");
    }

    private void BuildExistsSubquery(MemberExpression navAccess, LambdaExpression itemPredicate)
    {
        var navProp = (PropertyInfo)navAccess.Member;
        var parentParam = (ParameterExpression)navAccess.Expression;
        var parentAlias = GetAliasFor(parentParam);
        var parentType = parentParam.Type;

        var elementType = GetCollectionElementType(navProp.PropertyType);
        var parentKeyProp = parentType.GetProperties().FirstOrDefault(p => Attribute.IsDefined(p, typeof(KeyAttribute)));
        var fkProp = elementType?.GetProperty(parentType.Name + "Id");

        if (elementType == null || parentKeyProp == null || fkProp == null)
        {
            _sb.Append("/* Unsupported method: Any */");
            return;
        }

        var subAlias = UniqueAlias(_dialect.GetTableAlias(elementType));
        _parameterAliases[itemPredicate.Parameters[0]] = subAlias;

        _sb.Append("EXISTS (SELECT 1 FROM ")
            .Append(_dialect.QuoteIdentifier(_dialect.GetTableName(elementType)))
            .Append(" AS ").Append(_dialect.QuoteIdentifier(subAlias))
            .Append(" WHERE ")
            .Append(_dialect.QuoteIdentifier(subAlias)).Append(".").Append(_dialect.QuoteIdentifier(fkProp.Name))
            .Append(" = ")
            .Append(_dialect.QuoteIdentifier(parentAlias)).Append(".").Append(_dialect.QuoteIdentifier(parentKeyProp.Name))
            .Append(" AND ");
        Visit(itemPredicate.Body);
        _sb.Append(")");
    }

    private string GetAliasFor(ParameterExpression parameter)
    {
        if (!_parameterAliases.TryGetValue(parameter, out var alias))
        {
            alias = _rootAlias;
            _parameterAliases[parameter] = alias;
        }

        return alias;
    }

    private string UniqueAlias(string alias)
    {
        var candidate = alias;
        var suffix = 1;
        while (!_usedAliases.Add(candidate))
            candidate = alias + ++suffix;
        return candidate;
    }

    private static Type GetCollectionElementType(Type type)
    {
        if (type.IsGenericType && typeof(IEnumerable).IsAssignableFrom(type))
            return type.GetGenericArguments()[0];

        return type.GetInterfaces()
            .FirstOrDefault(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IEnumerable<>))
            ?.GetGenericArguments()[0];
    }

    private static Expression StripQuotes(Expression expression)
    {
        while (expression.NodeType == ExpressionType.Quote)
            expression = ((UnaryExpression)expression).Operand;
        return expression;
    }

    private static bool ContainsParameter(Expression expression)
    {
        var finder = new ParameterFinder();
        finder.Visit(expression);
        return finder.Found;
    }

    private sealed class ParameterFinder : ExpressionVisitor
    {
        public bool Found;

        protected override Expression VisitParameter(ParameterExpression node)
        {
            Found = true;
            return base.VisitParameter(node);
        }
    }

    private sealed class ParameterReplacer : ExpressionVisitor
    {
        private readonly ParameterExpression _target;
        private readonly Expression _replacement;

        public ParameterReplacer(ParameterExpression target, Expression replacement)
        {
            _target = target;
            _replacement = replacement;
        }

        protected override Expression VisitParameter(ParameterExpression node) =>
            node == _target ? _replacement : base.VisitParameter(node);
    }

    private void VisitStringContains(MethodCallExpression node)
    {
        var columnExpression = node.Object;
        var ignoreCase = false;
        if (columnExpression is MethodCallExpression { Method.Name: "ToLower" } toLowerCall)
        {
            columnExpression = toLowerCall.Object;
            ignoreCase = true;
        }

        var columnSql = Capture(columnExpression);
        var value = (string)Expression.Lambda(node.Arguments[0]).Compile().DynamicInvoke();
        _sb.Append(_dialect.BuildContainsPredicate(columnSql, value, ignoreCase));
    }

    private string Capture(Expression expression)
    {
        var mark = _sb.Length;
        Visit(expression);
        var captured = _sb.ToString(mark, _sb.Length - mark);
        _sb.Length = mark;
        return captured;
    }
}