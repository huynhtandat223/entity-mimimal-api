using CFW.AppHost.Features.Shared;
using CFW.Core.Dependencies;
using Microsoft.EntityFrameworkCore;

namespace CFW.AppHost.Features.Endpoints.Configurations;

public class ModuleInitializer : IModuleInitializer
{
    private readonly AppDbContext _db;
    public ModuleInitializer(AppDbContext db)
    {
        _db = db;
    }

    public async Task RunModule(IHost app)
    {
        if (app is not IEndpointRouteBuilder endpointRouteBuilder)
            throw new InvalidOperationException();

        var containerConfigurations = await _db
            .Set<Models.ContainerConfiguration>()
            .Include(x => x.Endpoints)
            .Where(x => x.Endpoints!.Any())
            .ToListAsync();

        foreach (var containerConfig in containerConfigurations)
        {
            var containerGroupBuilder = endpointRouteBuilder.MapGroup(containerConfig.RoutePrefix);
            containerGroupBuilder.MapGet("/{path}", (string path) =>
            {
                var endpoint = containerConfig.Endpoints!.FirstOrDefault(x => x.Path == $"/{path}"
                    && x.Method == Models.HttpMethod.GET);

                if (endpoint is null)
                    return Results.NotFound();

                endpoint.ContainerConfiguration = null;
                return Results.Ok(endpoint);
            });
        }
    }
}
