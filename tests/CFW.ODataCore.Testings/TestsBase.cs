using CFW.Core.Entities;
using CFW.CoreTestings.Logging;
using CFW.EntityApi;
using CFW.EntityApi.Models;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.OData.ModelBuilder;
using Testcontainers.MsSql;

namespace CFW.EntityMinimalApi.Testings;

public class TestsBase : IAsyncLifetime
{
    private readonly MsSqlContainer _msSqlContainer = new MsSqlBuilder().Build();

    protected readonly ITestOutputHelper _testOutputHelper;
    protected WebApplicationFactory<Program> _factory;
    protected List<object> requestObjects = new List<object>();

    public const string DefaultPassword = "123!@#abcABC";
    public const string DefaultIdProp = nameof(IEntity<Guid>.Id);

    public TestsBase(ITestOutputHelper testOutputHelper, AppFactory factory
        , string? odataPrefix = null, Type[]? types = null)
    {
        _testOutputHelper = testOutputHelper;
        _factory = factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureTestServices(services =>
            {
                var currentDirectory = Directory.GetCurrentDirectory();
                var dbDir = Path.Combine(currentDirectory, "testDbs");
                if (!Directory.Exists(dbDir))
                    Directory.CreateDirectory(dbDir);
                var dbPath = Path.Combine(dbDir, $"appdbcontext_{Guid.NewGuid()}.db");
                services.AddDbContext<TestingDbContext>(
                           options => options
                           .ReplaceService<IModelCustomizer, AutoScanModelCustomizer<TestingDbContext>>()
                           .EnableSensitiveDataLogging()
                           .UseSqlite($"Data Source={dbPath}"));

                services
                    .AddEntityApi(Constants.DefaultODataRoutePrefix)
                    .ConfigureODataModelBuilder(b => b.EnableLowerCamelCase())
                    .UseDbContext<TestingDbContext>();

                services.AddSingleton(requestObjects);

            }).ConfigureLogging(logging =>
            {
                logging.ClearProviders();
                logging.Services.AddSingleton<ILoggerProvider>(r
                    => new XunitLoggerProvider(_testOutputHelper, "Testing"));
            }); ;
        });
    }

    public Task DisposeAsync()
    {
        return _msSqlContainer.DisposeAsync().AsTask();
    }

    public Task InitializeAsync()
    {
        return _msSqlContainer.StartAsync();
    }
}
