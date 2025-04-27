using CFW.DynamicApi.Buiders;

namespace CFW.DynamicApi;

public interface IEndpointConfigurator
{
    DynamicEntityGroupBuilder Configure() => default!;

    Task<DynamicEntityGroupBuilder> ConfigureAsync() => default!;
}
