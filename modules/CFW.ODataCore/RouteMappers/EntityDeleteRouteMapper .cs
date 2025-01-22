using CFW.ODataCore.Intefaces;
using CFW.ODataCore.Models.Metadata;
using CFW.ODataCore.Models.Requests;
using Microsoft.AspNetCore.Mvc;

namespace CFW.ODataCore.RouteMappers;

public class EntityDeleteRouteMapper<TSource> : IRouteMapper
    where TSource : class
{
    private readonly IRouteMapper _internalRouteMapper;
    public EntityDeleteRouteMapper(MetadataEntity metadata, IServiceScopeFactory serviceScopeFactory)
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

public class EntityDeleteRouteMapper<TSource, TKey> : IRouteMapper
    where TSource : class
{
    private readonly MetadataEntity _metadata;

    public EntityDeleteRouteMapper(MetadataEntity metadata)
    {
        _metadata = metadata;
    }

    public Task MapRoutes(RouteGroupBuilder routeGroupBuilder)
    {
        var ignoreQueryOptions = _metadata.ODataQueryOptions.IgnoreQueryOptions;
        var keyPattern = this.GetKeyPattern<TKey>();

        routeGroupBuilder.MapPatch(keyPattern, async (TKey key
            , [FromServices] IServiceProvider serviceProvider
            , [FromServices] IEntityDeletionHandler<TSource, TKey> handler
            , CancellationToken cancellationToken) =>
        {
            var entityConfig = _metadata.GetOrCreateEndpointConfiguration<TSource>(serviceProvider);

            var command = new DeletionCommand<TSource, TKey>(key) { EntityConfiguration = entityConfig };
            var result = await handler.Handle(command, cancellationToken);
            return result.ToResults();
        });

        return Task.CompletedTask;
    }
}

