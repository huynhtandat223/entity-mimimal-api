using CFW.ODataCore.Intefaces;
using CFW.ODataCore.Models;
using CFW.ODataCore.Models.Metadata;
using Microsoft.AspNetCore.Mvc.Formatters;
using Microsoft.AspNetCore.OData.Formatter;
using Microsoft.AspNetCore.OData.Query;
using Microsoft.EntityFrameworkCore;
using Microsoft.OData;

namespace CFW.ODataCore.RouteMappers;

public class EntityQueryRouteMapper<TSource> : IRouteMapper
    where TSource : class
{
    private readonly MetadataEntity _metadata;

    public EntityQueryRouteMapper(MetadataEntity metadata, IServiceScopeFactory serviceScopeFactory)
    {
        _metadata = metadata;
    }

    public Task MapRoutes(RouteGroupBuilder routeGroupBuilder)
    {
        var ignoreQueryOptions = _metadata.ODataQueryOptions.IgnoreQueryOptions;

        routeGroupBuilder.MapGet($"/", async (HttpContext httpContext
            , ODataOutputFormatter formatter
            , CancellationToken cancellationToken) =>
        {
            if (_metadata.KeyProperty is null)
            {
                _metadata.InitSourceMetadata(httpContext.RequestServices);
            }

            var feature = _metadata.CreateOrGetODataFeature<TSource>();
            httpContext.Features.Set(feature);

            var dbContextProvider = httpContext.RequestServices.GetRequiredService<IDbContextProvider>();
            var db = dbContextProvider.GetDbContext();
            var queryable = db.Set<TSource>().AsNoTracking();

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
        }).Produces<TSource>();

        return Task.CompletedTask;
    }
}

