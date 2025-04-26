using CFW.AppHost.Features.Identity.Models;
using CFW.AppHost.Features.Shared;
using CFW.DynamicApi;

namespace CFW.AppHost.Features.Identity.Endpoints;

public class TenantsEndpointConfigurator : IEndpointConfigurator
{
    public void Configure(DynamicApiBuilder builder)
    {
        var routeGroup = builder.AddDbSetRouteGroup<Tenant, AppDbContext, Guid>(cfg =>
        {
            cfg.AllowListing(cfg =>
            {
                cfg.AddInterceptor<ODataFeatureInterceptor<Tenant>>();
            });
        });
    }
}
