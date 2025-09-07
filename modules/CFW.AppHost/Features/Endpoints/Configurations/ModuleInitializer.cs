using CFW.AppHost.Features.Endpoints.Services;
using CFW.Core.Dependencies;

namespace CFW.AppHost.Features.Endpoints.Configurations;

public class ModuleInitializer : IModuleInitializer
{
    private readonly RuntimeEndpointRegister _runtimeEndpointRegister;

    public ModuleInitializer(RuntimeEndpointRegister runtimeEndpointRegister)
    {
        _runtimeEndpointRegister = runtimeEndpointRegister;
    }

    public async Task InitModule(IHostApplicationBuilder builder)
    {
        builder.Services.AddOpenApi(o =>
        {
            o.AddOperationTransformer<RuntimeOpenApiQueryOperationTransformer>();
        });

        await Task.CompletedTask;
    }

    public async Task RunModule(IHost app)
    {
        if (app is not IEndpointRouteBuilder endpointRouteBuilder)
            throw new InvalidOperationException();

        await _runtimeEndpointRegister.ResiterEndpoints(endpointRouteBuilder);
    }
}
