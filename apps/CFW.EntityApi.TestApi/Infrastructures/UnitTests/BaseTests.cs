using CFW.Core.Entities;
using CFW.CoreTestings.DataGenerations;
using CFW.CoreTestings.Logging;
using CFW.EntityApi.Models;
using CFW.EntityApi.TestApi.Infrastructures.DbContexts;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
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
            builder.ConfigureTestServices(services =>
            {
                var currentDirectory = Directory.GetCurrentDirectory();
                var dbDir = Path.Combine(currentDirectory, "testDbs");
                if (!Directory.Exists(dbDir))
                    Directory.CreateDirectory(dbDir);
                var dbPath = Path.Combine(dbDir, $"appdbcontext_{Guid.NewGuid()}.db");
                services.AddDbContext<AppDbContext>(
                           options => options
                           .ReplaceService<IModelCustomizer, AutoScanModelCustomizer<AppDbContext>>()
                           .EnableSensitiveDataLogging()
                           .UseSqlite($"Data Source={dbPath}"));

                services
                    .AddEntityApi(Constants.DefaultODataRoutePrefix)
                    .ConfigureODataModelBuilder(b => b.EnableLowerCamelCase())
                    .UseDbContext<AppDbContext>();

                services.AddSingleton(requestObjects);

            }).ConfigureLogging(logging =>
            {
                logging.ClearProviders();
                logging.Services.AddSingleton<ILoggerProvider>(r
                    => new XunitLoggerProvider(_testOutputHelper, "Testing"));
            }); ;
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
