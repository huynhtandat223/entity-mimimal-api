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

        foreach (var apiGroup in _registry.ApiGroups)
        {
            var group = containerGroupBuilder
                .MapGroup(apiGroup.RouteName)
                .WithTags(apiGroup.RouteName);

            foreach (var operation in apiGroup.Operations)
            {
                operation.MapApi(group);
            }
        }
    }
}
