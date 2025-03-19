using CFW.EntityApi.Models;

namespace CFW.EntityApi.Attributes;

[Obsolete]

[AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
public class EntityAuthorizeAttribute : BaseRoutingAttribute
{
    public ApiMethod[]? ApplyMethods { get; set; }

    public string? Roles { get; set; }
}
