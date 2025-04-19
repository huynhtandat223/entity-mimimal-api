using CFW.EntityApi;
using CFW.EntityApi.Models;
using CFW.ODataCore;
using CFW.ODataCore.Features.Identity.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.OData.ModelBuilder;

var builder = WebApplication.CreateBuilder(args);

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
        .PopuplateEntityFrameworkEntities<AppDbContext>();


//Authentication
builder.Services.AddAuthorization();
builder.Services.AddIdentityApiEndpoints<ApplicationUser>()
    .AddRoles<TenantRole>()
    .AddEntityFrameworkStores<AppDbContext>();

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