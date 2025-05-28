using CFW.AppHost.Features.Endpoints.Infrastructures;
using CFW.Core.Dependencies;

namespace CFW.AppHost.Features.Endpoints.Configurations;

public class ModuleInitializer : IModuleInitializer
{
    private readonly RuntimeEndpointRegister _runtimeEndpointRegister;

    public ModuleInitializer(RuntimeEndpointRegister runtimeEndpointRegister)
    {
        _runtimeEndpointRegister = runtimeEndpointRegister;
    }

    public async Task RunModule(IHost app)
    {
        if (app is not IEndpointRouteBuilder endpointRouteBuilder)
            throw new InvalidOperationException();

        await _runtimeEndpointRegister.ResiterEndpoints(endpointRouteBuilder);
    }
}
