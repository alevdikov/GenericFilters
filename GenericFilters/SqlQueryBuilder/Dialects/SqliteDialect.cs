namespace GenericFilters.SqlQueryBuilder.Dialects;

public class SqliteDialect : AnsiSqlDialect
{
    public override string BuildContainsPredicate(string columnSql, string value, bool ignoreCase) =>
        ignoreCase
            ? $"instr(lower({columnSql}), '{EscapeString(value.ToLowerInvariant())}') > 0"
            : $"instr({columnSql}, '{EscapeString(value)}') > 0";
}