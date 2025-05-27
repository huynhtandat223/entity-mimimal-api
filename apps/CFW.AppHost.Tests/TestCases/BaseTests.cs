using CFW.AppHost.Features.Endpoints;
using CFW.AppHost.Features.Shared;
using CFW.CoreTestings.Logging;
using CFW.DynamicApi.Entensions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Xunit.Abstractions;

namespace CFW.AppHost.Tests.TestCases;

public class BaseTests
{
    protected WebApplicationFactory<Program> _factory;
    protected readonly ITestOutputHelper _testOutputHelper;


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

                services.AddDbContext<AppDbContext>(options =>
                    options.UseSqlite($"Data Source={dbPath}"));

                services
                    .AddDynamicApi(Constants.DefaultTestingRoutePrefix, container =>
                    {
                        container.DefaultPageSize = 50;
                    }, typeof(EndpointsCreate).Assembly);

                //services.AddSingleton(requestObjects);
            }).ConfigureLogging(logging =>
            {
                logging.ClearProviders();
                logging.Services.AddSingleton<ILoggerProvider>(r
                    => new XunitLoggerProvider(_testOutputHelper, "Testing"));
            }); ;
        });
    }
}
