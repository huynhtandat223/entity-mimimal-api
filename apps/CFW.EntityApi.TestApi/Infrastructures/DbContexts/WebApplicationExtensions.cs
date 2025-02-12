namespace CFW.EntityApi.TestApi.Infrastructures.DbContexts;

public static class WebApplicationExtensions
{
    public static void MigrateDatabase(this WebApplication app)
    {

        using var scope = app.Services.CreateScope();
        var db = scope.ServiceProvider.GetService<AppDbContext>();
        if (db is not null && !db.Database.CanConnect())
        {
            db.Database.EnsureCreated();
            //var supperAdminName = "admin@gmail.com";
            //var supperAdminRole = "SuperAdmin";
            //var supperAdminPassword = "123!@#abcABC";
            //var systemTenant = new Tenant { Name = "System", Type = TenantType.System };
            //db.Set<Tenant>().Add(systemTenant);
            //await db.SaveChangesAsync();

            //using var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            //using var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<TenantRole>>();

            //var supperAdminUser = new ApplicationUser { UserName = supperAdminName, Email = supperAdminName };
            //var result = await userManager.CreateAsync(supperAdminUser, supperAdminPassword);
            //if (!result.Succeeded)
            //{
            //    throw new InvalidOperationException("Test data invalid. User creation failed.");
            //}

            //var role = await roleManager.CreateAsync(new TenantRole(systemTenant.Id, supperAdminRole));
            //if (!role.Succeeded)
            //{
            //    throw new InvalidOperationException("Test data invalid. Role creation failed.");
            //}

            //supperAdminUser.TenantRoles.Add(new TenantRole(systemTenant.Id, supperAdminRole));
            //await db.SaveChangesAsync();
        }


    }
}
