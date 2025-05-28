using Microsoft.EntityFrameworkCore;

namespace CFW.AppHost.Features.Endpoints.Infrastructures;

public class RuntimeDbContext : DbContext
{
    private readonly RuntimeTypeRegistry _runtimeTypeRegistry;

    public RuntimeDbContext(DbContextOptions<RuntimeDbContext> options, RuntimeTypeRegistry runtimeTypeRegistry) : base(options)
    {
        _runtimeTypeRegistry = runtimeTypeRegistry;
    }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        // Register all runtime types as entities in the DbContext
        foreach (var type in _runtimeTypeRegistry.RuntimeTypes)
        {
            builder.Entity(type);
        }
    }
}
