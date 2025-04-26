using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
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

        foreach (var op in _registry.GetAllOperations())
        {
            var group = containerGroupBuilder.MapGroup(op.Route);

            op.ConfigureRoute?.Invoke(group);

            group.MapMethods("", new[] { op.HttpMethod }, async ctx =>
            {
                foreach (var factory in op.InterceptorFactories)
                {
                    var interceptor = factory(ctx.RequestServices);
                    var earlyResult = await interceptor.OnExecutingAsync(ctx, op);
                    if (earlyResult is not null)
                    {
                        await ctx.Response.WriteAsJsonAsync(earlyResult);
                        return;
                    }
                }

                var result = await op.Handler!(ctx);

                foreach (var factory in op.InterceptorFactories)
                {
                    var interceptor = factory(ctx.RequestServices);
                    result = await interceptor.OnExecutedAsync(ctx, op, result);
                }

                await ctx.Response.WriteAsJsonAsync(result);
            });
        }
    }
}
