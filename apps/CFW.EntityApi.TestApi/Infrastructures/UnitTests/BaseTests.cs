using CFW.Core.Entities;
using CFW.CoreTestings.Logging;
using CFW.EntityApi.Models.Builders;
using CFW.EntityApi.Registrators;
using CFW.EntityApi.TestApi.Infrastructures.DbContexts;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.OData;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.OData.ModelBuilder;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace CFW.EntityApi.TestApi.Infrastructures.UnitTests;

public class BaseTests
{
    protected readonly ITestOutputHelper _testOutputHelper;
    protected WebApplicationFactory<Program> _factory;
    protected List<object> requestObjects = new List<object>();

    public const string DefaultPassword = "123!@#abcABC";
    public const string DefaultIdProp = nameof(IEntity<Guid>.Id);

    public int DefaultPageSize = 10;

    public JsonSerializerOptions DefaultJsonSeriallizerOptions = new JsonSerializerOptions
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) }
    };

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

    protected WebApplicationFactory<Program> SetupEntityApi(string routePrefix
        , DataProvider dataProvider
        , Action<ODataOptions>? odataOptionSetup = null
        , Action<ContainerApiBuilder>? containerApiBuilderSetup = null)
    {
        return _factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureTestServices(services =>
            {
                services.Configure<DbContextSetting>(o =>
                {
                    if (dataProvider == DataProvider.SqlServer)
                    {

                        o.SqlServerConnectionString
                            = $"";
                    }

                    if (dataProvider == DataProvider.Sqlite)
                    {
                        var currentDirectory = Directory.GetCurrentDirectory();
                        var dbDir = Path.Combine(currentDirectory, "testDbs");
                        if (!Directory.Exists(dbDir))
                            Directory.CreateDirectory(dbDir);
                        var dbPath = Path.Combine(dbDir, $"appdbcontext_{Guid.NewGuid()}.db");

                        o.SqliteConnectionString = $"Data Source={dbPath}";
                    }
                });

                var entityApiBuilder = services
                    .AddEntityMinimalApi(routePrefix);
                entityApiBuilder.ConfigureODataModelBuilder(b => b.EnableLowerCamelCase());
                entityApiBuilder.PopuplateEntityFrameworkEntities<AppDbContext>();
                entityApiBuilder.UseEntityAttributes();
                containerApiBuilderSetup?.Invoke(entityApiBuilder);

                if (odataOptionSetup is not null)
                {
                    entityApiBuilder.ConfigureODataOptions(odataOptionSetup);
                }
            });
        });
    }

    public async Task<List<T>> SeedDataIfNotAnyRecords<T>(int count, DbContext? db = null)
        where T : class
    {
        db ??= GetDbContext();

        var existingData = await db.Set<T>().ToListAsync();
        if (existingData.Any())
            return existingData;

        var data = DataGenerator.CreateList<T>(count);
        foreach (var item in data)
        {
            db.Add(item);
        }
        await db.SaveChangesAsync();

        return await db.Set<T>().ToListAsync();
    }

    public async Task<List<object>> SeedDataIfNotAnyRecords(Type dbType, int count, DbContext? db = null)
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

    protected async Task SeedUsers(IEnumerable<SeedUserInfo> seedUserInfos)
    {
        using var scope = _factory.Services.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<IdentityUser>>();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
        foreach (var seedUserInfo in seedUserInfos)
        {
            var creatingUser = new IdentityUser { UserName = seedUserInfo.UserName };
            var result = await userManager.CreateAsync(creatingUser, seedUserInfo.Password);
            result.Succeeded.Should().BeTrue();

            if (seedUserInfo.Roles != null)
            {
                var user = await userManager.FindByNameAsync(seedUserInfo.UserName);
                user.Should().NotBeNull();

                foreach (var role in seedUserInfo.Roles)
                {
                    var roleExists = await roleManager.RoleExistsAsync(role);
                    if (!roleExists)
                    {
                        var creatingRole = new IdentityRole { Name = role };
                        var roleResult = await roleManager.CreateAsync(creatingRole);
                        roleResult.Succeeded.Should().BeTrue();
                    }
                }

                await userManager.AddToRolesAsync(user!, seedUserInfo.Roles);
            }
        }
    }

    protected async Task SeedUser(string userName, string password)
    {
        using var scope = _factory.Services.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<IdentityUser>>();
        var user = new IdentityUser { UserName = userName };
        var result = await userManager.CreateAsync(user, password);
        if (!result.Succeeded)
            throw new InvalidOperationException("Test data invalid. User creation failed.");
    }

    protected AppDbContext GetDbContext()
        => _factory.Services.CreateScope().ServiceProvider.GetRequiredService<AppDbContext>();

    public static IEnumerable<TestData> GetTestData(string routePrefix)
    {
        var categoryData = new TestData<Category>
        {
            Url = $"{routePrefix}/categories",
            RoutePrefix = routePrefix,
            DataProvider = DataProvider.Sqlite
        };

        yield return categoryData;

        yield return new TestData<Product>
        {
            DataProvider = DataProvider.Sqlite,
            Url = $"{routePrefix}/products",
            RoutePrefix = routePrefix
        };
    }

    public static IEnumerable<TestData> GetSpecificType(string routePrefix, Type specificType)
        => GetTestData(routePrefix).Where(x => x.GetType().GetGenericArguments()[0] == specificType);
}
