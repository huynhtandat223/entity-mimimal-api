using CFW.EntityApi.Attributes;
using CFW.EntityApi.Models.Builders;
using CFW.EntityApi.Queries;
using Microsoft.AspNetCore.OData;
using Microsoft.AspNetCore.OData.Abstracts;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.Extensions.Options;
using Microsoft.OData.ModelBuilder;
using Microsoft.OData.UriParser;
using System.Reflection;

namespace CFW.EntityApi.Registrators;

public class ContainerMemberRegistrationContext
{
    public ContainerRegistrationContext ContainerRegistrationContext { get; set; } = null!;

    public RouteGroupBuilder MemberRouteGroup { get; set; } = null!;

    public string Name { get; set; } = string.Empty;

    public IODataFeature? ODataFeature { get; set; }

    public ODataOptions ODataOptions { get; set; } = null!;
    public List<EntityActionAttribute> Actions { get; internal set; } = new List<EntityActionAttribute>();

    internal EntityApiConfiguration EntityConfiguration { get; set; } = null!;

    [Obsolete("Is this needed?")]
    internal IContainerMemberRouter QueryMemberRouter { get; set; } = null!;

    [Obsolete("Is this needed?")]
    internal IContainerMemberRouter CreationMemberRouter { get; set; } = null!;

    public IODataFeature CreateODataFeature<TEntity>(IServiceProvider serviceProvider
        , IEntityType? entityType
        , PropertyInfo? keyPropertyInfo)
        where TEntity : class
    {
        if (ODataFeature is not null)
            return ODataFeature;

        var builder = new ODataConventionModelBuilder();
        var entitySet = builder.EntitySet<TEntity>(Name);
        var routePrefix = ContainerRegistrationContext.ContainerConfiguration.RoutePrefix;

        var odataEntityType = builder.AddEntityType(typeof(TEntity));
        builder.AddEntitySet(Name, odataEntityType);

        if (keyPropertyInfo is not null)
            odataEntityType.HasKey(keyPropertyInfo);

        //TODO: handle this in a better way
        builder.EnableLowerCamelCaseForPropertiesAndEnums();

        var model = builder.GetEdmModel();
        var edmEntitySet = model.EntityContainer.FindEntitySet(Name);
        var entitySetSegment = new EntitySetSegment(edmEntitySet);
        var segments = new List<ODataPathSegment> { entitySetSegment };

        var path = new ODataPath(segments);
        var optionsFactory = serviceProvider.GetRequiredService<IOptionsFactory<ODataOptions>>();
        var odataOptions = optionsFactory.Create(routePrefix);

        if (odataOptions.QueryConfigurations.MaxTop is null
            || odataOptions.QueryConfigurations.MaxTop.Value < 1)
            odataOptions.QueryConfigurations.MaxTop = ContainerRegistrationContext.ContainerConfiguration.DefaultPageSize;

        ContainerRegistrationContext.ContainerConfiguration.ConfigureODataOptions?.Invoke(odataOptions);

        if (!odataOptions.RouteComponents.TryGetValue(routePrefix, out var routeComponent))
            odataOptions = odataOptions.AddRouteComponents(routePrefix, model);

        ODataOptions = odataOptions;
        ODataFeature = new ODataFeature
        {
            Path = path,
            Model = model,
            RoutePrefix = routePrefix,
            Services = odataOptions.RouteComponents[routePrefix].ServiceProvider
        };

        return ODataFeature;
    }
}
