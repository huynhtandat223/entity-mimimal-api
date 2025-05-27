using CFW.AppHost.Features.Endpoints.Configurations;
using CFW.AppHost.Features.Shared;
using CFW.Core.Builders.RuntimeTypeBuilders;
using CFW.DynamicApi;
using CFW.DynamicApi.Entensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.EntityFrameworkCore.Migrations.Design;
using Microsoft.EntityFrameworkCore.Sqlite.Design.Internal;
using Microsoft.Extensions.Options;
using System.Reflection;
using System.Reflection.Emit;
using Endpoint = CFW.AppHost.Features.Endpoints.Models.Endpoint;

namespace CFW.AppHost.Features.Endpoints;

[ApiOperation("endpoints")]
public class EndpointsCreate : IRequestHandler<Endpoint, Endpoint>
{
    private readonly RuntimeAsmConfig _runtimeAsmConfig;
    private readonly AppDbContext _db;

    public EndpointsCreate(IOptions<RuntimeAsmConfig> runtimeAsmConfig, AppDbContext db)
    {
        _runtimeAsmConfig = runtimeAsmConfig.Value;
        _db = db;
    }

    public async Task<IResult<Endpoint>> Handle(RequestModel<Endpoint> request, CancellationToken cancellationToken)
    {
        var endpoint = request.Model;
        if (endpoint.RuntimeEntityDefinition is null)
        {
            throw new NotImplementedException("RuntimeEntityDefinition is not implemented yet.");
        }

        var typeDef = endpoint.RuntimeEntityDefinition;
        var fullTypeName = string.Join('.', typeDef.Namespace, typeDef.Name);

        var assemblyBuilder = new PersistedAssemblyBuilder(new AssemblyName(typeDef.Namespace)
            , typeof(object).Assembly);
        var moduleBuilder = assemblyBuilder.DefineDynamicModule("MainModule");

        var propDefs = typeDef.Properties.Select(x => new RuntimePropertyDefinition
        {
            Name = x.Name,
            Type = Type.GetType(x.Type)!,
            IsKey = x.IsKey,
            IsRequired = x.IsRequired
        });
        var typeName = string.Join('.', typeDef.Namespace, typeDef.Name);
        var type = RuntimeTypeBuilder.CreateType(
            new RuntimeTypeDefinition
            {
                TypeName = typeName,
                ModuleBuilder = moduleBuilder
            }, propDefs);

        var fullPath = Path.Combine(_runtimeAsmConfig.GetRuntimeEntitiesDirOrDefault(), fullTypeName + ".dll");
        assemblyBuilder.Save(fullPath);

        //migrate db
        var assemblyBytes = File.ReadAllBytes(fullPath);
        var loadedAssembly = Assembly.Load(assemblyBytes);
        var loadedType = loadedAssembly.GetType(fullTypeName);

        var services = new ServiceCollection();
        var connectionString = _db.Database.GetConnectionString();
        var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseSqlite(connectionString)
                .Options;
        using var migrateDb = new AppDbContext(options, [loadedType]);

        services.AddSingleton(migrateDb);
        services.AddEntityFrameworkDesignTimeServices();
        services.AddDbContextDesignTimeServices(migrateDb);

        var designTimeServices = new SqliteDesignTimeServices();
        designTimeServices.ConfigureDesignTimeServices(services);
        var serviceProvider = services.BuildServiceProvider();
        var scaffolder = serviceProvider.GetRequiredService<IMigrationsScaffolder>();
        var migration = scaffolder.ScaffoldMigration(loadedType.Name, "EFCoreDesign");

        var projectDir = Directory.GetCurrentDirectory();
        var outputDir = Path.Combine(projectDir, "Migrations");
        scaffolder.Save(projectDir, migration, outputDir);


        var result = await request.CreateEntity<Endpoint, AppDbContext>();
        return result.Created();
    }
}