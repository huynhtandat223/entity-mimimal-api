using CFW.AppHost.Features.Identity.Models;
using CFW.AppHost.Features.Shared;
using CFW.DynamicApi;
using CFW.DynamicApi.Buiders;
using CFW.DynamicApi.Interceptors;

namespace CFW.AppHost.Features.Identity.EndpointConfigurators;

public class TenantsEndpointConfigurator : IEndpointConfigurator
{
    private readonly DynamicEntityGroupBuilder<Tenant, AppDbContext> _buider;

    public TenantsEndpointConfigurator(DynamicEntityGroupBuilder<Tenant, AppDbContext> builder)
    {
        _buider = builder;
    }

    public DynamicEntityGroupBuilder Configure()
    {
        return _buider
            .AddQueryApi(api =>
            {
                api.UseInterceptor<ODataFeatureInterceptor<Tenant>>();
            })
            //.AddCreationApi()
            .ExcludeProperties(x => x.ConnectionString);
    }
}
