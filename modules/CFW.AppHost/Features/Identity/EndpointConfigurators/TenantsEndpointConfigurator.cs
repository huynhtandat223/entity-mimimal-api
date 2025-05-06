using CFW.AppHost.Features.Identity.Models;
using CFW.AppHost.Features.Shared;
using CFW.DynamicApi;
using CFW.DynamicApi.Buiders;
using CFW.DynamicApi.Interceptors;

namespace CFW.AppHost.Features.Identity.EndpointConfigurators;

public class TenantsEndpointConfigurator : IEndpointConfigurator
{
    private readonly DynamicEntityGroupBuilder<Tenant, AppDbContext, Guid> _buider;

    public TenantsEndpointConfigurator(DynamicEntityGroupBuilder<Tenant, AppDbContext, Guid> builder)
    {
        _buider = builder;
    }

    public DynamicEntityGroupBuilder Configure()
    {
        return _buider
            .AddUpdatingApi(api =>
            {
                api.ExcludeProperties<Tenant>(t => t.Id, t => t.Policies, t => t.Roles, t => t.TenantUsers);
            })
            .AddGetSingleApi(api =>
            {
                api.UseInterceptor<ODataFeatureInterceptor<Tenant>>();
            })
            .AddPartialUpdatingApi()
            .AddDeleteApi()
            .AddQueryApi(api =>
            {
                api.UseInterceptor<ODataFeatureInterceptor<Tenant>>();
            })
            .AddCreationApi(api =>
            {
                api.ExcludeProperties<Tenant>(t => t.Policies, t => t.Roles, t => t.TenantUsers);
            })
            .ExcludeProperties(x => x.CreatedAt, x => x.UpdatedAt!, x => x.ConnectionString);
    }
}
