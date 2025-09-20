using CFW.AppHost.Infrastructures.DbContextExtensions.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.EntityFrameworkCore.Scaffolding.Metadata;
using Microsoft.EntityFrameworkCore.Sqlite.Design.Internal;
using Microsoft.EntityFrameworkCore.SqlServer.Design.Internal;

namespace CFW.AppHost.Infrastructures.DbContextExtensions.Utils;

public static class DbContextUtils
{
    public static IServiceProvider CreateDesignTimeServiceProvider(DbContext db)
    {
        var services = new ServiceCollection();
        var databaseProvider = GetDatabaseProvider(db.Database.ProviderName);

        IDesignTimeServices designTimeServices;
        if (databaseProvider == DatabaseProvider.MSSQL)
        {
#pragma warning disable EF1001 // Internal EF Core API usage.
            designTimeServices = new SqlServerDesignTimeServices();
            designTimeServices.ConfigureDesignTimeServices(services);
#pragma warning restore EF1001 // Internal EF Core API usage.
        }

        if (databaseProvider == DatabaseProvider.Sqlite)
        {
#pragma warning disable EF1001 // Internal EF Core API usage.
            designTimeServices = new SqliteDesignTimeServices();
            designTimeServices.ConfigureDesignTimeServices(services);
#pragma warning restore EF1001 // Internal EF Core API usage.
        }

        services.AddEntityFrameworkDesignTimeServices();
        services.AddDbContextDesignTimeServices(db);

        var serviceProvider = services.BuildServiceProvider();

        return serviceProvider;
    }

    public static DatabaseProvider GetDatabaseProvider(string? providerName) =>
        providerName switch
        {
            "Microsoft.EntityFrameworkCore.SqlServer" => DatabaseProvider.MSSQL,
            "Npgsql.EntityFrameworkCore.PostgreSQL" => DatabaseProvider.PostgreSQL,
            "Pomelo.EntityFrameworkCore.MySql" or "MySql.Data.EntityFrameworkCore" => DatabaseProvider.MySQL,
            "Oracle.EntityFrameworkCore" => DatabaseProvider.Oracle,
            "Microsoft.EntityFrameworkCore.Sqlite" => DatabaseProvider.Sqlite,
            _ => throw new NotSupportedException($"Database provider '{providerName}' is not supported."),
        };

    public static Type GetClrType(this DatabaseColumn databaseColumn)
    {
        var annotations = databaseColumn.GetAnnotations();
        var runtimeTypeObj = annotations.FirstOrDefault(a => a.Name == "ClrType")?.Value;

        if (runtimeTypeObj is null)
        {
            if(databaseColumn.StoreType == "TEXT")
                return typeof(string);

            throw new NotImplementedException($"Can't get ClrType for column '{databaseColumn.Name}' with StoreType '{databaseColumn.StoreType}'");
        }

        var fullName = runtimeTypeObj.ToString();
        var type = Type.GetType(fullName!);

        if (type is null)
            throw new NotImplementedException($"Can't get ClrType for column '{databaseColumn.Name}' with StoreType '{databaseColumn.StoreType}'");

        return type;
    }
}
