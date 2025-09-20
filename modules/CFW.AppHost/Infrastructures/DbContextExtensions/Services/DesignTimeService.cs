using CFW.AppHost.Infrastructures.DbContextExtensions.Models;
using CFW.Core.Dependencies;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.EntityFrameworkCore.Sqlite.Design.Internal;
using Microsoft.EntityFrameworkCore.SqlServer.Design.Internal;

namespace CFW.AppHost.Infrastructures.DbContextExtensions.Services;

/// <summary>
/// https://learn.microsoft.com/en-us/ef/core/cli/services
/// </summary>
public class DesignTimeService : ISingletonService
{
    public IServiceProvider CreateDesignTimeServiceProvider(string connectionString, DatabaseProvider databaseProvider)
    {
        var services = new ServiceCollection();

        var optionsBuilder = new DbContextOptionsBuilder<TempDbContext>();
        IDesignTimeServices designTimeServices;
        if (databaseProvider == DatabaseProvider.MSSQL)
        {
            optionsBuilder.UseSqlServer(connectionString);

#pragma warning disable EF1001 // Internal EF Core API usage.
            designTimeServices = new SqlServerDesignTimeServices();
            designTimeServices.ConfigureDesignTimeServices(services);
#pragma warning restore EF1001 // Internal EF Core API usage.
        }

        if (databaseProvider == DatabaseProvider.Sqlite)
        {
            optionsBuilder.UseSqlite(connectionString);
#pragma warning disable EF1001 // Internal EF Core API usage.
            designTimeServices = new SqliteDesignTimeServices();
            designTimeServices.ConfigureDesignTimeServices(services);
#pragma warning restore EF1001 // Internal EF Core API usage.
        }

        var dbContext = new TempDbContext(optionsBuilder.Options);

        services.AddEntityFrameworkDesignTimeServices();
        services.AddDbContextDesignTimeServices(dbContext);

        var serviceProvider = services.BuildServiceProvider();

        return serviceProvider;
    }
}
