using Microsoft.OData.ModelBuilder;

namespace CFW.EntityApi.Registrators;

public class ContainerConfiguration
{
    internal string RoutePrefix { get; set; } = string.Empty;

    internal bool IsDefault { get; set; } = true;

    /// <summary>
    /// Configure model builder when all entities are added, before call builder.GetEdmModel()
    /// </summary>
    internal Action<ODataConventionModelBuilder>? ConfigureModelBuilder { get; set; }

    /// <summary>
    /// Configure minimal api container route group
    /// </summary>
    internal Action<RouteGroupBuilder>? ConfigureContainerRouteGroup { get; set; }
}
