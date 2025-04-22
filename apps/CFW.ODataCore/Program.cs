using CFW.Core.Dependencies;
using CFW.EntityApi;
using CFW.EntityApi.Models;
using CFW.EntityApi.Models.Builders;
using CFW.ODataCore;
using CFW.ODataCore.Features.Identity.Models;
using Microsoft.AspNetCore.Identity;
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
            var runtimeAsmConfig = sp.GetRequiredService<IOptions<RuntimeAsmConfig>>().Value;
            var endpoints = db.Set<CFW.ODataCore.Features.Endpoints.Models.Endpoint>()
            .AsNoTracking()
            .ToList();

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
    .AddRoles<TenantRole>()
    .AddEntityFrameworkStores<AppDbContext>();

builder.Services.TryAddAllServices();

var app = builder.Build();

app.UseAuthorization();
app.MapIdentityApi<ApplicationUser>();

app.UseEntityMinimalApi();

using var scope = app.Services.CreateScope();
var db = scope.ServiceProvider.GetService<AppDbContext>();
if (db is not null && !db.Database.CanConnect())
{
    db.Database.EnsureCreated();
    var supperAdminName = "admin@gmail.com";
    var supperAdminRole = "SuperAdmin";
    var supperAdminPassword = "123!@#abcABC";
    var systemTenant = new Tenant { Name = "System", Type = TenantType.System };
    db.Set<Tenant>().Add(systemTenant);
    await db.SaveChangesAsync();

    using var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
    using var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<TenantRole>>();

    var supperAdminUser = new ApplicationUser { UserName = supperAdminName, Email = supperAdminName };
    var result = await userManager.CreateAsync(supperAdminUser, supperAdminPassword);
    if (!result.Succeeded)
    {
        throw new InvalidOperationException("Test data invalid. User creation failed.");
    }

    var role = await roleManager.CreateAsync(new TenantRole(systemTenant.Id, supperAdminRole));
    if (!role.Succeeded)
    {
        throw new InvalidOperationException("Test data invalid. Role creation failed.");
    }

    supperAdminUser.TenantRoles.Add(new TenantRole(systemTenant.Id, supperAdminRole));
    await db.SaveChangesAsync();
}

app.Run();