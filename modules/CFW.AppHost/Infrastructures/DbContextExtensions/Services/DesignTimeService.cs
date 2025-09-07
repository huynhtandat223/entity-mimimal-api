using CFW.AppHost.Infrastructures.DbContextExtensions.Models;
using CFW.Core.Dependencies;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
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
        optionsBuilder.UseSqlServer(connectionString);

        if (databaseProvider == DatabaseProvider.MSSQL)
            optionsBuilder.UseSqlServer(connectionString);

        var dbContext = new TempDbContext(optionsBuilder.Options);

        services.AddEntityFrameworkDesignTimeServices();
        services.AddDbContextDesignTimeServices(dbContext);

#pragma warning disable EF1001 // Internal EF Core API usage.
        var designTimeServices = new SqlServerDesignTimeServices();
        designTimeServices.ConfigureDesignTimeServices(services);
#pragma warning restore EF1001 // Internal EF Core API usage.

        var serviceProvider = services.BuildServiceProvider();

        return serviceProvider;
    }
}
