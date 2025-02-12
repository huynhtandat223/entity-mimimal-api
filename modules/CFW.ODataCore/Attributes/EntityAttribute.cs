using CFW.EntityApi.Models;
using Microsoft.AspNetCore.OData.Query;

namespace CFW.EntityApi.Attributes;

[AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
public class EntityAttribute(string? name = null) : BaseRoutingAttribute
{
    /// <summary>
    /// If not set, the name will generate from global builder.
    /// </summary>
    public string? Name { get; set; } = name;

    public ApiMethod[]? Methods { get; set; }

    public AllowedQueryOptions? QueryOptions { get; set; }

    public AllowedQueryOptions? GetByKeyOptions { get; set; }

    internal Type TargetType { get; set; } = null!;
}
