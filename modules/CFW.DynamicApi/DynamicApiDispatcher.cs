using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Routing;

namespace CFW.DynamicApi;

public class DynamicApiDispatcher
{
    private readonly DynamicApiRegistry _registry;
    private readonly ContainerConfiguration _containerConfiguration;

    public DynamicApiDispatcher(DynamicApiRegistry registry, ContainerConfiguration containerConfiguration)
    {
        _registry = registry;
        _containerConfiguration = containerConfiguration;
    }

    public void MapEndpoints(IEndpointRouteBuilder app)
    {
        var containerGroupBuilder = app.MapGroup(_containerConfiguration.RoutePrefix);

        foreach (var apiGroup in _registry.ApiGroups)
        {
            var group = containerGroupBuilder
                .MapGroup(apiGroup.RouteName)
                .WithTags(apiGroup.RouteName);

            foreach (var operation in apiGroup.Operations)
            {
                RegisterApiOperation(group, operation);
            }
        }
    }

    public static void RegisterApiOperation(RouteGroupBuilder group, DynamicApiOperation operation)
    {
        var route = group.MapMethods(operation.Route, [operation.HttpMethod], async (HttpContext ctx) =>
        {
            // Initialize interceptors once
            var interceptors = operation.InterceptorFactories
                .Select(factory => factory(ctx.RequestServices))
                .ToList();

            // Execute OnExecutingAsync for all interceptors
            foreach (var interceptor in interceptors)
            {
                await interceptor.OnExecutingAsync(ctx, operation);
            }

            // Execute the handler
            var result = await operation.Handler!(ctx);

            // Execute OnExecutedAsync for all interceptors
            foreach (var interceptor in interceptors)
            {
                await interceptor.OnExecutedAsync(ctx, operation, result);
            }

            // Use the static instance of EmptyHttpResult instead of creating a new one
            return EmptyHttpResult.Instance;
        }).WithMetadata(operation);
    }
}
