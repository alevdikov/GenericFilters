using System;
using System.Globalization;

namespace GenericFilters.SqlQueryBuilder.Dialects;

/// <summary>
/// Baseline ANSI SQL dialect. Database-specific dialects should derive from this
/// and override only the members whose ANSI behaviour they can't use as-is.
/// </summary>
public class AnsiSqlDialect : ISqlDialect
{
    public virtual string QuoteIdentifier(string identifier) => $"\"{identifier}\"";

    public virtual string FormatLiteral(object value)
    {
        switch (value)
        {
            case null:
                return "NULL";
            case string s:
                return $"'{EscapeString(s)}'";
            case DateTime dt:
                return $"'{dt:yyyy-MM-dd HH:mm:ss}'";
            case bool b:
                return b ? "TRUE" : "FALSE";
            case double d:
                return FormatFloatingPoint(d);
            case float f:
                return FormatFloatingPoint(f);
            default:
                return Convert.ToString(value, CultureInfo.InvariantCulture);
        }
    }

    public virtual string GetTableName(Type modelType) => Pluralize(modelType.Name);

    public virtual string GetTableAlias(Type modelType) => modelType.Name.Substring(0, 1).ToLowerInvariant();

    public virtual string BuildContainsPredicate(string columnSql, string value, bool ignoreCase) =>
        ignoreCase
            ? $"LOWER({columnSql}) LIKE '%{EscapeString(value.ToLowerInvariant())}%'"
            : $"{columnSql} LIKE '%{EscapeString(value)}%'";

    protected static string EscapeString(string value) => value.Replace("'", "''");

    protected static string FormatFloatingPoint(double value) =>
        value.ToString("0.0###############", CultureInfo.InvariantCulture);

    private static string Pluralize(string name)
    {
        if (name.EndsWith("y", StringComparison.Ordinal) && name.Length > 1 && !IsVowel(name[name.Length - 2]))
            return name.Substring(0, name.Length - 1) + "ies";

        if (name.EndsWith("s", StringComparison.Ordinal) || name.EndsWith("x", StringComparison.Ordinal) ||
            name.EndsWith("ch", StringComparison.Ordinal) || name.EndsWith("sh", StringComparison.Ordinal))
            return name + "es";

        return name + "s";
    }

    private static bool IsVowel(char c) => "aeiouAEIOU".IndexOf(c) >= 0;
}