using CFW.Core.Entities;
using CFW.CoreTestings.DataGenerations;
using CFW.CoreTestings.Logging;
using CFW.EntityApi.Registrators;
using CFW.EntityApi.TestApi.Infrastructures.DbContexts;
using Microsoft.AspNetCore.TestHost;
using Microsoft.OData.ModelBuilder;
using Xunit.Abstractions;

namespace CFW.EntityApi.TestApi.Infrastructures.UnitTests;

public class BaseTests
{
    protected readonly ITestOutputHelper _testOutputHelper;
    protected WebApplicationFactory<Program> _factory;
    protected List<object> requestObjects = new List<object>();

    public const string DefaultPassword = "123!@#abcABC";
    public const string DefaultIdProp = nameof(IEntity<Guid>.Id);

    public BaseTests(ITestOutputHelper testOutputHelper, AppFactory factory
        , string? odataPrefix = null, Type[]? types = null)
    {
        _testOutputHelper = testOutputHelper;
        _factory = factory.WithWebHostBuilder(builder =>
        {
            builder
            .ConfigureTestServices(services =>
            {
                var containerConfigsServices = services
                .Where(x => x.ImplementationInstance is not null
                    && x.ImplementationInstance.GetType() == typeof(ContainerConfiguration))
                .ToList();

                foreach (var containerConfigService in containerConfigsServices)
                {
                    services.Remove(containerConfigService);

                    var containerConfig = (ContainerConfiguration)containerConfigService.ImplementationInstance!;
                    var key = containerConfig.RoutePrefix;
                    var keyedServices = services.Where(x => x.IsKeyedService && x.ServiceKey!.Equals(key)).ToList();
                    foreach (var keyedService in keyedServices)
                    {
                        services.Remove(keyedService);
                    }
                }
                services.AddSingleton(requestObjects);

            })
            .ConfigureLogging(logging =>
            {
                logging.ClearProviders();
                logging.Services.AddSingleton<ILoggerProvider>(r
                    => new XunitLoggerProvider(_testOutputHelper, "Testing"));
            }); ;
        });
    }

    protected WebApplicationFactory<Program> SetupEntityApi(string routePrefix)
    {
        return _factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureTestServices(services =>
            {
                var currentDirectory = Directory.GetCurrentDirectory();
                var dbDir = Path.Combine(currentDirectory, "testDbs");
                if (!Directory.Exists(dbDir))
                    Directory.CreateDirectory(dbDir);
                var dbPath = Path.Combine(dbDir, $"appdbcontext_{Guid.NewGuid()}.db");

                services.Configure<DbContextSetting>(o =>
                {
                    o.SqliteConnectionString = $"Data Source={dbPath}";
                });

                services
                    .AddEntityApi(routePrefix)
                    .ConfigureODataModelBuilder(b => b.EnableLowerCamelCase())
                    .UseDbContext<AppDbContext>();
            });
        });
    }

    public async Task<List<object>> SeedData(Type dbType, int count, AppDbContext? db = null)
    {
        db ??= GetDbContext();
        var data = DataGenerator.CreateList(dbType, count);
        foreach (var item in data)
        {
            db.Add(item);
        }
        await db.SaveChangesAsync();
        return data.OfType<object>().ToList();
    }

    protected AppDbContext GetDbContext()
        => _factory.Services.CreateScope().ServiceProvider.GetRequiredService<AppDbContext>();
}
