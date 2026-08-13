using System.Reflection;

namespace GenericFilters.SqlQueryBuilder;

public sealed class JoinInfo
{
    public PropertyInfo NavProperty { get; set; }
    public PropertyInfo ForeignKeyProperty { get; set; }
    public PropertyInfo KeyProperty { get; set; }
    public string Alias { get; set; }
}