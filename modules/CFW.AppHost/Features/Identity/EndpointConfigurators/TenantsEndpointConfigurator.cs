using CFW.AppHost.Features.Identity.Models;
using CFW.AppHost.Features.Shared;
using CFW.DynamicApi;
using CFW.DynamicApi.Buiders;
using CFW.DynamicApi.Interceptors;

namespace CFW.AppHost.Features.Identity.EndpointConfigurators;

public class TenantsEndpointConfigurator : IEndpointConfigurator
{
    public DynamicEntityGroupBuilder Configure()
    {
        var builder = DynamicEntityGroupBuilder.Create<Tenant, AppDbContext>()
            .AddQueryApi(api =>
            {
                api.UseInterceptor<ODataFeatureInterceptor<Tenant>>();
            })
            .AddCreationApi();

        return builder;
    }
}
