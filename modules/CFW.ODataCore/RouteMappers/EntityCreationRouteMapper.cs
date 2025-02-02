using CFW.ODataCore.Intefaces;
using CFW.ODataCore.Models;
using CFW.ODataCore.Models.Deltas;
using CFW.ODataCore.Models.Metadata;
using CFW.ODataCore.Models.Requests;
using Microsoft.AspNetCore.Mvc;

namespace CFW.ODataCore.RouteMappers;


public class EntityCreationRouteMapper<TSource> : IRouteMapper
where TSource : class, new()
{
    private readonly MetadataEntity _metadataEntity;
    private readonly EntityEndpoint<TSource> _entityConfig;

    public EntityCreationRouteMapper(MetadataEntity metadataEntity, IServiceProvider serviceProvider)
    {
        _metadataEntity = metadataEntity;
        _entityConfig = _metadataEntity.GetOrCreateEndpointConfiguration<TSource>(serviceProvider);
    }

    public Task MapRoutes(RouteGroupBuilder routeGroupBuilder)
    {
        routeGroupBuilder.MapPost("/", async (EntityDelta<TSource> delta
            , [FromServices] IEntityCreationHandler<TSource> entityCreationHandler
            , CancellationToken cancellationToken) =>
        {
            var command = new CreationCommand<TSource>(delta);
            var result = await entityCreationHandler.Handle(command, cancellationToken);
            return result.ToResults();
        }).Accepts<TSource>("application/json");

        return Task.CompletedTask;
    }
}

