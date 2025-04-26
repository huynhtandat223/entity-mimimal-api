using CFW.Core.Dependencies;
using CFW.EntityApi;
using CFW.EntityApi.Models;
using CFW.EntityApi.Models.Builders;
using CFW.ODataCore;
using CFW.ODataCore.Features.Identity.Models;
using CFW.ODataCore.Features.Identity.Services.Extensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.Options;
using Microsoft.OData.ModelBuilder;
using System.Runtime.Loader;

var builder = WebApplication.CreateBuilder(args);

builder.Services.Configure<RuntimeAsmConfig>(
    builder.Configuration.GetSection(nameof(RuntimeAsmConfig)));

builder.Services.AddDbContext<AppDbContext>(
               (s, options) =>
               {
                   options.EnableSensitiveDataLogging()
                   .ReplaceService<IModelCustomizer, AutoScanModelCustomizer<AppDbContext>>();

                   options.UseSqlite("Data Source=database.db");
               });

builder.Services
        .AddEntityMinimalApi("api")
        .ConfigureODataModelBuilder(b => b.EnableLowerCamelCase())
        .AddApiConfigurations<IServiceProvider>(sp =>
        {
            var db = sp.GetRequiredService<AppDbContext>();
            var endpoints = new List<CFW.ODataCore.Features.Endpoints.Models.Endpoint>();
            try
            {
                endpoints = db.Set<CFW.ODataCore.Features.Endpoints.Models.Endpoint>()
                .AsNoTracking()
                .ToList();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Exception: {ex.ToString()}");
                return Enumerable.Empty<EntityApiConfiguration>();
            }

            var runtimeAsmConfig = sp.GetRequiredService<IOptions<RuntimeAsmConfig>>().Value;


            if (!endpoints.Any())
                return Enumerable.Empty<EntityApiConfiguration>();

            var runtimeEntitiesDir = runtimeAsmConfig.GetRuntimeEntitiesDirOrDefault();
            var assemblyFiles = Directory.GetFiles(runtimeEntitiesDir, "*.dll", SearchOption.AllDirectories);
            if (assemblyFiles.Length == 0)
                return Enumerable.Empty<EntityApiConfiguration>();

            var result = new List<EntityApiConfiguration>();
            foreach (var assemblyPath in assemblyFiles)
            {
                var assembly = AssemblyLoadContext.Default.LoadFromAssemblyPath(assemblyPath);
                foreach (var endpointConfig in endpoints)
                {
                    var entityType = assembly.GetTypes().Single();
                    var entityConfigurationType = typeof(EntityApiConfiguration<,,>)
                        .MakeGenericType(typeof(AppDbContext), entityType, typeof(Guid));

                    var dbEntityType = db.Model.FindEntityType(entityType);
                    var keyProperty = dbEntityType!.FindPrimaryKey()!.Properties.Single();

                    var entityConfiguration = (EntityApiConfiguration)ActivatorUtilities.CreateInstance(sp, entityConfigurationType
                        , dbEntityType, keyProperty);

                    entityConfiguration.RouteName = endpointConfig.Path;

                    result.Add(entityConfiguration);
                }
            }

            return result;
        })
        .PopuplateEntityFrameworkEntities<AppDbContext>(entitiesSelector: x => false); //disable auto gen api for all entities

//Authentication
builder.Services.AddAuthorization();
builder.Services.AddIdentityApiEndpoints<ApplicationUser>()
    .AddRoles<ApplicationRole>()
    .AddEntityFrameworkStores<AppDbContext>();

builder.Services.TryAddAllServices();

var app = builder.Build();

app.UseAuthorization();
app.MapIdentityApi<ApplicationUser>();

app.UseEntityMinimalApi();

using var scope = app.Services.CreateScope();
var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
await db.SeedSuperAdminAsync();

app.Run();