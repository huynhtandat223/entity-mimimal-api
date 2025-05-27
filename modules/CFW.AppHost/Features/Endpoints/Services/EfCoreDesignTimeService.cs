using CFW.Core.Dependencies;
using CFW.Core.Results;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.EntityFrameworkCore.Migrations.Design;
using Microsoft.EntityFrameworkCore.SqlServer.Design.Internal;


namespace CFW.ODataCore.Features.Endpoints.Services;

public class EfCoreDesignTimeService : IScopedService
{
    private readonly AppDbContext _db;
    public EfCoreDesignTimeService(AppDbContext db)
    {
        _db = db;
    }

    public Task<Result> CreateMigration()
    {
        var services = new ServiceCollection();
        services.AddSingleton(_db);
        services.AddEntityFrameworkDesignTimeServices();
        services.AddDbContextDesignTimeServices(_db);

        var sqlDesignTime = new SqlServerDesignTimeServices();
        sqlDesignTime.ConfigureDesignTimeServices(services);

        var serviceProvider = services.BuildServiceProvider();
        var scaffolder = serviceProvider.GetRequiredService<IMigrationsScaffolder>();
        var migration = scaffolder.ScaffoldMigration("MyMigration", "EFCoreDesign");

        var projectDir = Directory.GetCurrentDirectory();
        var outputDir = Path.Combine(projectDir, "Migrations");
        scaffolder.Save(projectDir, migration, outputDir);

        return Task.FromResult(new Result { IsSuccess = true });
    }
}
