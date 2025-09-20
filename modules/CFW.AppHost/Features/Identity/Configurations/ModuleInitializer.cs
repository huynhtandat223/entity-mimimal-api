using CFW.AppHost.Features.Core;
using CFW.AppHost.Features.Identity.Models;
using CFW.AppHost.Features.Identity.Services.Extensions;
using CFW.Core.Dependencies;
using Microsoft.AspNetCore.Identity;

namespace CFW.AppHost.Features.Identity.Configurations;

public class ModuleInitializer : IModuleInitializer
{
    public async Task InitModule(IHostApplicationBuilder builder)
    {
        builder.Services.AddAuthorization();
        builder.Services.AddIdentityApiEndpoints<ApplicationUser>()
            .AddRoles<ApplicationRole>()
            .AddEntityFrameworkStores<AppDbContext>();

        builder.Services.AddScoped<IUserClaimsPrincipalFactory<ApplicationUser>, ApplicationUserClaimsPrincipalFactory>();
        await Task.CompletedTask;
    }

    public async Task RunModule(WebApplication app)
    {
        var appBuilder = (IApplicationBuilder)app;
        appBuilder.UseAuthorization();
        app.MapIdentityApi<ApplicationUser>();

        using var scope = app.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await db.SeedSuperAdminAsync();

        await Task.CompletedTask;
    }
}
