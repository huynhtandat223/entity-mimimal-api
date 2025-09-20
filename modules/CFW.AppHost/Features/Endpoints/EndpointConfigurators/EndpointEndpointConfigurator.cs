
using CFW.AppHost.Features.Core;
using CFW.DynamicApi;
using CFW.DynamicApi.Buiders;
using CFW.DynamicApi.Interceptors.OData;
using Endpoint = CFW.AppHost.Features.Endpoints.Models.Endpoint;

namespace CFW.AppHost.Features.Endpoints.EndpointConfigurators;

public class EndpointEndpointConfigurator : IEndpointConfigurator
{
    private readonly DynamicEntityGroupBuilder<Endpoint, AppDbContext, Guid> _buider;

    public EndpointEndpointConfigurator(DynamicEntityGroupBuilder<Endpoint, AppDbContext, Guid> builder)
    {
        _buider = builder;
    }

    public DynamicEntityGroupBuilder Configure()
    {
        return _buider
            .AddQueryApi(api =>
            {
                api.UseInterceptor<ODataFeatureInterceptor<Endpoint>>();
            });
    }
}
