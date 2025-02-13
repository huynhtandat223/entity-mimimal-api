using CFW.Core.Utils;
using CFW.EntityApi.Models;

namespace CFW.EntityApi.Attributes;

[AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
public class ActionAttribute : BaseRoutingAttribute
{
    public ApiMethod Method { get; set; } = ApiMethod.Post;

    internal Type TargetType { get; set; } = null!;

    internal Type InterfaceType { get; set; } = null!;

    public string ActionName { get; set; }

    public ActionAttribute(string routeName)
    {
        ActionName = StringUtils.SanitizeRoute(routeName);
    }
}

public class EntityActionAttribute : ActionAttribute
{
    public string EntityName { get; set; }

    public EntityActionAttribute(string routeName) : base(routeName)
    {
        var segments = StringUtils.SanitizeRoute(routeName).Split('/');
        if (segments.Length < 2)
            throw new ArgumentException("Route name must contain at least two segments.");

        EntityName = segments.First();
        ActionName = string.Join('/', segments.Skip(1));
    }
}