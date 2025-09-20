using CFW.Core.Dependencies;
using Microsoft.CodeAnalysis;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

namespace CFW.AppHost.Infrastructures.DbContextExtensions.Services.Runtimes;

public class RuntimeDbMigrator : IScopedService
{
    //private readonly RuntimeTypeRegistry _runtimeTypeRegistry;
    //public RuntimeDbMigrator(RuntimeTypeRegistry runtimeTypeRegistry)
    //{
    //    _runtimeTypeRegistry = runtimeTypeRegistry;
    //}

    //public Task CreateAndStoreMigration(string migrationName, string folderPath)
    //{
    //    //create migration
    //    var services = new ServiceCollection();
    //    var connectionString = _db.Database.GetConnectionString();
    //    var options = new DbContextOptionsBuilder<RuntimeDbContext>()
    //        .UseSqlite(connectionString)
    //        .Options;

    //    using var migrateDb = new RuntimeDbContext(options, _runtimeTypeRegistry);

    //    services.AddSingleton(migrateDb);
    //    services.AddEntityFrameworkDesignTimeServices();
    //    services.AddDbContextDesignTimeServices(migrateDb);

    //    var designTimeServices = new SqliteDesignTimeServices();
    //    designTimeServices.ConfigureDesignTimeServices(services);
    //    var serviceProvider = services.BuildServiceProvider();
    //    var scaffolder = serviceProvider.GetRequiredService<IMigrationsScaffolder>();
    //    var migration = scaffolder.ScaffoldMigration(migrationName, "EFCoreDesign");

    //    var projectDir = Directory.GetCurrentDirectory();
    //    var outputDir = Path.Combine(projectDir, "Migrations");
    //    scaffolder.Save(projectDir, migration, outputDir);

    //    return Task.CompletedTask;
    //}

    public async Task<string> GenerateMigrationScript(DbContext migrateDb)
    {       
        // Ensure the model is built (triggers OnModelCreating with the new entity from _runtimeTypeRegistry)
        _ = migrateDb.Model;

        // Get the service provider from the DbContext for additional services
        var sp = migrateDb.GetInfrastructure();

        // Get the current design-time model (target model)
        var designTimeModelService = sp.GetRequiredService<IDesignTimeModel>();
        var targetModel = designTimeModelService.Model;

        // Get the snapshot model (source model from previous migration)
        var migrationsAssembly = sp.GetRequiredService<IMigrationsAssembly>();
        var snapshotModel = migrationsAssembly.ModelSnapshot?.Model;

        // Finalize and initialize the snapshot model if it exists (required for accurate diffing)
        if (snapshotModel is IMutableModel mutableModel)
        {
            snapshotModel = mutableModel.FinalizeModel();
        }

        if (snapshotModel != null)
        {
            var modelRuntimeInitializer = migrateDb.GetService<IModelRuntimeInitializer>();
            snapshotModel = modelRuntimeInitializer.Initialize(snapshotModel);
        }

        // Compute differences (MigrationOperations) - use null for snapshotModel if first migration (empty source)
        var modelDiffer = sp.GetRequiredService<IMigrationsModelDiffer>();
        var operations = modelDiffer.GetDifferences(
            snapshotModel?.GetRelationalModel(), // Source: previous snapshot (null for initial)
            targetModel.GetRelationalModel()     // Target: current model
        );

        if (!operations.Any())
        {
            throw new InvalidOperationException("No migration operations detected. No changes to apply.");
        }

        // Generate SQL from operations
        var sqlGenerator = sp.GetRequiredService<IMigrationsSqlGenerator>();
        var sqlCommands = sqlGenerator.Generate(operations, targetModel);
        string sqlScript = string.Join(Environment.NewLine,
            sqlCommands.Select(c => c.CommandText + ";"));

        // Save the SQL to a file (optional, in the specified folderPath)
        //var outputDir = Path.Combine(Directory.GetCurrentDirectory(), folderPath);
        //Directory.CreateDirectory(outputDir);
        //var sqlFilePath = Path.Combine(outputDir, $"{migrationName}.sql");
        //await File.WriteAllTextAsync(sqlFilePath, sqlScript);

        return await Task.FromResult(sqlScript);

    }

    //public async Task ApplyMigration()
    //{
    //    var applyServices = new ServiceCollection();
    //    var connectionString = _db.Database.GetConnectionString();

    //    applyServices.AddDbContext<RuntimeDbContext>(options =>
    //    {
    //        options.UseSqlite(connectionString, x =>
    //        {
    //            // Must match the migration assembly
    //            x.MigrationsAssembly(typeof(RuntimeDbContext).Assembly.GetName().Name);
    //        });
    //    });

    //    applyServices.AddSingleton(_runtimeTypeRegistry);

    //    var applyProvider = applyServices.BuildServiceProvider();

    //    using var scope = applyProvider.CreateScope();
    //    var dbContext = scope.ServiceProvider.GetRequiredService<RuntimeDbContext>();

    //    await dbContext.Database.MigrateAsync();

    //}
}
