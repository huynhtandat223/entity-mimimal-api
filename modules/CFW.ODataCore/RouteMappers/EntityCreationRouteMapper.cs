using CFW.ODataCore.Intefaces;
using CFW.ODataCore.Models.Deltas;
using CFW.ODataCore.Models.Requests;
using Microsoft.AspNetCore.Mvc;

namespace CFW.ODataCore.RouteMappers;

public class EntityCreationRouteMapper<TSource> : IRouteMapper
where TSource : class, new()
{
    public Task MapRoutes(RouteGroupBuilder routeGroupBuilder)
    {
        routeGroupBuilder.MapPost("/", async (HttpContext httpContext
            , EntityDelta<TSource> delta
            , [FromServices] IEntityCreationHandler<TSource> entityCreationHandler
            , CancellationToken cancellationToken) =>
        {
            var command = new CreationCommand<TSource>(delta);
            var result = await entityCreationHandler.Handle(command, cancellationToken);
            return result.ToResults();
        });

        return Task.CompletedTask;
    }
}

