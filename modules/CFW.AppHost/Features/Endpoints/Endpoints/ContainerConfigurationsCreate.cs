using CFW.AppHost.Features.Shared;
using CFW.DynamicApi;

namespace CFW.AppHost.Features.Endpoints.Endpoints;

[ApiOperation("endpoints/container-configurations")]
public class ContainerConfigurationsCreate : IRequestHandler<ContainerConfiguration, Models.ContainerConfiguration>
{
    public async Task<IResult<Models.ContainerConfiguration>> Handle(RequestModel<ContainerConfiguration> request
        , CancellationToken cancellationToken)
    {
        var entity = new Models.ContainerConfiguration
        {
            RoutePrefix = request.Model.RoutePrefix,
            DefaultPageSize = request.Model.DefaultPageSize
        };

        var db = request.ServiceProvider.GetRequiredService<AppDbContext>();
        db.Set<Models.ContainerConfiguration>().Add(entity);
        await db.SaveChangesAsync();

        return entity.Created();
    }
}
