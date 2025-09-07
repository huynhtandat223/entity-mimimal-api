using CFW.AppHost.Features.Endpoints.Services;
using CFW.AppHost.Features.Shared;
using CFW.DynamicApi;
using CFW.DynamicApi.Entensions;
using Endpoint = CFW.AppHost.Features.Endpoints.Models.Endpoint;

namespace CFW.AppHost.Features.Endpoints.Endpoints;

[ApiOperation("endpoints")]
public class EndpointsCreate : IRequestHandler<Endpoint, Endpoint>
{
    private readonly RuntimeTypeRegistry _runtimeTypeRegistry;
    private readonly RuntimeDbMigrator _runtimeDbMigrator;

    public EndpointsCreate(RuntimeDbMigrator runtimeDbMigrator, RuntimeTypeRegistry runtimeTypeRegistry)
    {
        _runtimeTypeRegistry = runtimeTypeRegistry;
        _runtimeDbMigrator = runtimeDbMigrator;
    }

    public async Task<IResult<Endpoint>> Handle(RequestModel<Endpoint> request, CancellationToken cancellationToken)
    {
        var endpoint = request.Model;
        if (endpoint.RuntimeEntityDefinition is null)
            throw new NotImplementedException("RuntimeEntityDefinition is not implemented yet.");

        var loadedType = _runtimeTypeRegistry.CreateTypeAddLoad(endpoint.RuntimeEntityDefinition);

        //Create migration
        var projectDir = Directory.GetCurrentDirectory();
        var outputDir = Path.Combine(projectDir, "Migrations");
        var migrationName = $"{loadedType.Name}_{DateTime.Now.ToIso8601String()}";
        await _runtimeDbMigrator.CreateMigration(migrationName, outputDir);

        //apply migration
        await _runtimeDbMigrator.ApplyMigration();

        //Save entity
        var result = await request.CreateEntity<Endpoint, AppDbContext>();
        return result.Created();
    }
}