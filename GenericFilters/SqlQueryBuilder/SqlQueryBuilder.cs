using System;
using System.Linq.Expressions;
using GenericFilters.SqlQueryBuilder.Dialects;

namespace GenericFilters.SqlQueryBuilder;

public class SqlQueryBuilder<TModel> where TModel : class
{
    public string BuildQuery(Expression<Func<TModel, bool>> filter, ISqlDialect dialect = null)
    {
        dialect ??= new AnsiSqlDialect();

        var rootAlias = dialect.GetTableAlias(typeof(TModel));
        var joins = new JoinResolver(typeof(TModel), dialect, rootAlias).Resolve(filter.Body);

        var selectClause = new SelectClauseVisitor<TModel>(dialect, rootAlias).Build(joins);
        var whereClause = new SqlExpressionVisitor(dialect, rootAlias, joins).Build(filter.Body);

        return $"{selectClause}\n{whereClause}";
    }
}