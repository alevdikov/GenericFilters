namespace GenericFilters.SqlQueryBuilder.Dialects;

public interface ISqlDialect
{
    string QuoteIdentifier(string identifier);

    string FormatLiteral(object value);

    string GetTableName(System.Type modelType);

    string GetTableAlias(System.Type modelType);

    string BuildContainsPredicate(string columnSql, string value, bool ignoreCase);
}