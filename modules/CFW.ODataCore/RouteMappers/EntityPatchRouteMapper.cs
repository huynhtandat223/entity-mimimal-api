using CFW.ODataCore.Intefaces;
using CFW.ODataCore.Models.Deltas;
using CFW.ODataCore.Models.Metadata;
using CFW.ODataCore.Models.Requests;
using Microsoft.AspNetCore.Mvc;

namespace CFW.ODataCore.RouteMappers;


public class EntityPatchRouteMapper<TSource> : IRouteMapper
    where TSource : class
{
    private readonly IRouteMapper _internalRouteMapper;
    public EntityPatchRouteMapper(MetadataEntity metadata, IServiceScopeFactory serviceScopeFactory)
    {
        //TODO: consider to use lazy initialization
        if (metadata.KeyProperty is null)
        {
            using var scope = serviceScopeFactory.CreateScope();
            metadata.InitSourceMetadata(scope.ServiceProvider);
        }

        var internalRouteMapperType = typeof(EntityPatchRouteMapper<,>)
            .MakeGenericType(typeof(TSource), metadata.KeyProperty!.PropertyInfo!.PropertyType);
        _internalRouteMapper = (IRouteMapper)Activator.CreateInstance(internalRouteMapperType, metadata)!;
    }

    public Task MapRoutes(RouteGroupBuilder routeGroupBuilder)
    {
        return _internalRouteMapper.MapRoutes(routeGroupBuilder);
    }
}

public class EntityPatchRouteMapper<TSource, TKey> : IRouteMapper
    where TSource : class
{
    private readonly MetadataEntity _metadata;

    public EntityPatchRouteMapper(MetadataEntity metadata)
    {
        _metadata = metadata;
    }

    public Task MapRoutes(RouteGroupBuilder routeGroupBuilder)
    {
        var ignoreQueryOptions = _metadata.ODataQueryOptions.IgnoreQueryOptions;
        var keyPattern = this.GetKeyPattern<TKey>();

        routeGroupBuilder.MapPatch(keyPattern, async (HttpContext httpContext
            , EntityDelta<TSource> delta
            , TKey key
            , [FromServices] IEntityPatchHandler<TSource, TKey> handler
            , CancellationToken cancellationToken) =>
        {
            var command = new PatchCommand<TSource, TKey>(delta, key);
            var result = await handler.Handle(command, cancellationToken);
            return result.ToResults();
        });

        return Task.CompletedTask;
    }
}

