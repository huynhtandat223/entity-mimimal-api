using CFW.ODataCore.Intefaces;
using CFW.ODataCore.Models.Metadata;
using Microsoft.AspNetCore.Mvc.Formatters;
using Microsoft.AspNetCore.OData;
using Microsoft.AspNetCore.OData.Formatter;
using Microsoft.AspNetCore.OData.Query;
using Microsoft.Extensions.Options;

namespace CFW.ODataCore.RouteMappers;

public class EntityQueryRouteMapper<TSource> : IRouteMapper
where TSource : class
{
    private readonly MetadataEntity _metadata;
    private readonly IServiceProvider serviceProvider;
    private readonly ODataOptions _oDataOptions;

    public EntityQueryRouteMapper(MetadataEntity metadata
        , IServiceProvider serviceProvider
        , IOptions<ODataOptions> oDataOptions)
    {
        _metadata = metadata;
        this.serviceProvider = serviceProvider;
        _oDataOptions = oDataOptions.Value;
    }

    public Task MapRoutes(RouteGroupBuilder routeGroupBuilder)
    {
        var endpointConfig = _metadata.GetOrCreateEndpointConfiguration<TSource>(serviceProvider);

        routeGroupBuilder.MapGet("/", async (HttpContext httpContext
            , ODataOutputFormatter formatter
            , CancellationToken cancellationToken) =>
        {
            var feature = _metadata.CreateOrGetODataFeature<TSource>(httpContext.RequestServices);
            httpContext.Features.Set(feature);

            var entityConfig = _metadata.GetOrCreateEndpointConfiguration<TSource>(httpContext.RequestServices);

            var queryable = await entityConfig.GetQueryable(httpContext.RequestServices);

            var ignoreQueryOptions = _metadata.ODataQueryOptions.IgnoreQueryOptions;
            var odataQueryContext = new ODataQueryContext(feature.Model, typeof(TSource), feature.Path);
            var options = new ODataQueryOptions<TSource>(odataQueryContext, httpContext.Request);

            var result = options.ApplyTo(queryable, ignoreQueryOptions);

            var formatterContext = new OutputFormatterWriteContext(httpContext,
                (stream, encoding) => new StreamWriter(stream, encoding),
                result.GetType() ?? typeof(object), result)
            {
                ContentType = "application/json;odata.metadata=none",
            };

            await formatter.WriteAsync(formatterContext);


        }).AddODataMetadata<TSource>(_metadata, _oDataOptions, endpointConfig);

        return Task.CompletedTask;
    }
}

public class CustomParameterMetadata
{
    public string Name { get; }
    public string Description { get; }
    public bool Required { get; }

    public CustomParameterMetadata(string name, string description, bool required)
    {
        Name = name;
        Description = description;
        Required = required;
    }
}

