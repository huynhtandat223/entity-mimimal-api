using CFW.EntityApi.Models;
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

    internal IContainerMemberRouter MemberRouter { get; set; } = null!;

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

        builder.EnableLowerCamelCaseForPropertiesAndEnums();

        var model = builder.GetEdmModel();
        var edmEntitySet = model.EntityContainer.FindEntitySet(Name);
        var entitySetSegment = new EntitySetSegment(edmEntitySet);
        var segments = new List<ODataPathSegment> { entitySetSegment };

        var path = new ODataPath(segments);
        var odataOptions = serviceProvider.GetRequiredService<IOptions<ODataOptions>>().Value;

        if (!odataOptions.RouteComponents.TryGetValue(routePrefix, out var routeComponent))
            odataOptions = odataOptions.AddRouteComponents(routePrefix, model);

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

public class ContainerRegistrationContext
{
    private readonly IEndpointRouteBuilder _endpointRouteBuilder;
    private readonly ITypesResolver _typeResolver;

    public ContainerRegistrationContext(WebApplication app, ContainerConfiguration containerConfiguration
        , ITypesResolver typeResolver)
    {
        _endpointRouteBuilder = app;
        RootServiceProvider = app.Services;
        ContainerConfiguration = containerConfiguration;
        _typeResolver = typeResolver;
    }

    internal RouteGroupBuilder ContainerGroupRoute { get; set; } = null!;

    public ContainerConfiguration ContainerConfiguration { get; set; } = null!;

    public IServiceProvider RootServiceProvider { get; }

    public ITypesResolver TypeResolver => _typeResolver;

    internal void ConfigureContainerRoutes()
    {
        ContainerGroupRoute = _endpointRouteBuilder.MapGroup(ContainerConfiguration.RoutePrefix);
        ContainerConfiguration.ConfigureContainerRouteGroup?.Invoke(ContainerGroupRoute);
    }
}
