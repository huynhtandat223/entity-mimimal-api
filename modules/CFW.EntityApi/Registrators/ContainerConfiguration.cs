using CFW.EntityApi.Models.Builders;
using Microsoft.AspNetCore.OData;
using Microsoft.AspNetCore.OData.Query;
using Microsoft.OData.ModelBuilder;

namespace CFW.EntityApi.Registrators;

public class ContainerConfiguration
{
    public AllowedQueryOptions AllowedQueryOptions { get; internal set; } = AllowedQueryOptions.All;

    public string RoutePrefix { get; internal set; } = string.Empty;

    public bool IsDefault { get; internal set; } = true;

    public int DefaultPageSize { get; internal set; } = 10;

    /// <summary>
    /// Configure model builder when all entities are added, before call builder.GetEdmModel()
    /// </summary>
    internal Action<ODataConventionModelBuilder>? ConfigureModelBuilder { get; set; }

    internal Action<ODataOptions>? ConfigureODataOptions { get; set; }

    /// <summary>
    /// Configure minimal api container route group
    /// </summary>
    internal Action<RouteGroupBuilder>? ConfigureContainerRouteGroup { get; set; }

    internal IList<IApiFeature> ApiFeatures { get; } = new List<IApiFeature>();

    public List<Type> CustomEntityImplementations { get; } = new List<Type>();

    public Func<IServiceProvider, IEnumerable<EntityApiConfiguration>>? ApiConfigurationsFunc { get; set; }
}
