using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.EntityFrameworkCore.Migrations.Design;
using Microsoft.EntityFrameworkCore.SqlServer.Design.Internal;
using Microsoft.Extensions.DependencyInjection;

namespace CFW.Core.EfCoreExtensions;

public static class DesignTimeService
{
    public static void CreateMigration(this DbContext db, string migrationName, string rootNamespace = "EFCoreDesign")
    {
        var services = new ServiceCollection();

        services.AddEntityFrameworkDesignTimeServices();
        services.AddDbContextDesignTimeServices(db);


#pragma warning disable EF1001 // Internal EF Core API usage.
        var designTimeServices = new SqlServerDesignTimeServices();
        designTimeServices.ConfigureDesignTimeServices(services);
#pragma warning restore EF1001 // Internal EF Core API usage.

        var serviceProvider = services.BuildServiceProvider();
        var scaffolder = serviceProvider.GetRequiredService<IMigrationsScaffolder>();
        var migration = scaffolder.ScaffoldMigration(migrationName, rootNamespace);

        var projectDir = Directory.GetCurrentDirectory();
        var outputDir = Path.Combine(projectDir, "Migrations");
        scaffolder.Save(projectDir, migration, outputDir);
    }
}
