using CFW.Core.Dependencies;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.EntityFrameworkCore.Migrations.Design;
using Microsoft.EntityFrameworkCore.Sqlite.Design.Internal;

namespace CFW.AppHost.Features.Endpoints.Services;

public class RuntimeDbMigrator : IScopedService
{
    private readonly RuntimeTypeRegistry _runtimeTypeRegistry;
    private readonly RuntimeDbContext _db;
    public RuntimeDbMigrator(RuntimeTypeRegistry runtimeTypeRegistry)
    {
        _runtimeTypeRegistry = runtimeTypeRegistry;
    }

    public Task CreateMigration(string migrationName, string folderPath)
    {
        //create migration
        var services = new ServiceCollection();
        var connectionString = _db.Database.GetConnectionString();
        var options = new DbContextOptionsBuilder<RuntimeDbContext>()
            .UseSqlite(connectionString)
            .Options;

        using var migrateDb = new RuntimeDbContext(options, _runtimeTypeRegistry);

        services.AddSingleton(migrateDb);
        services.AddEntityFrameworkDesignTimeServices();
        services.AddDbContextDesignTimeServices(migrateDb);

        var designTimeServices = new SqliteDesignTimeServices();
        designTimeServices.ConfigureDesignTimeServices(services);
        var serviceProvider = services.BuildServiceProvider();
        var scaffolder = serviceProvider.GetRequiredService<IMigrationsScaffolder>();
        var migration = scaffolder.ScaffoldMigration(migrationName, "EFCoreDesign");

        var projectDir = Directory.GetCurrentDirectory();
        var outputDir = Path.Combine(projectDir, "Migrations");
        scaffolder.Save(projectDir, migration, outputDir);

        return Task.CompletedTask;
    }

    public async Task ApplyMigration()
    {
        var applyServices = new ServiceCollection();
        var connectionString = _db.Database.GetConnectionString();

        applyServices.AddDbContext<RuntimeDbContext>(options =>
        {
            options.UseSqlite(connectionString, x =>
            {
                // Must match the migration assembly
                x.MigrationsAssembly(typeof(RuntimeDbContext).Assembly.GetName().Name);
            });
        });

        applyServices.AddSingleton(_runtimeTypeRegistry);

        var applyProvider = applyServices.BuildServiceProvider();

        using var scope = applyProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<RuntimeDbContext>();

        await dbContext.Database.MigrateAsync();

    }
}
