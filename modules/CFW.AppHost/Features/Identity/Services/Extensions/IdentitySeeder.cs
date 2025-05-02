using CFW.AppHost.Features.Identity.Models;
using CFW.AppHost.Features.Shared;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace CFW.AppHost.Features.Identity.Services.Extensions;

public static class IdentitySeeder
{
    public static async Task SeedSuperAdminAsync(this AppDbContext context)
    {
        await context.Database.EnsureCreatedAsync();
        await context.Database.MigrateAsync();

        var supperAdminName = "admin@gmail.com";
        var supperAdminRole = "SuperAdmin";
        var supperAdminPassword = "123!@#abcABC";

        // Ensure Tenant exists
        var systemTenant = await context.Tenants
            .FirstOrDefaultAsync(x => x.Name == "System");

        if (systemTenant == null)
        {
            systemTenant = new Tenant
            {
                Id = Guid.NewGuid(),
                Name = "System",
                ConnectionString = string.Empty, // Nếu cần dynamic thì xử lý sau
                CreatedAt = DateTime.UtcNow
            };
            context.Tenants.Add(systemTenant);
            await context.SaveChangesAsync();
        }

        // Ensure Role exists
        var superAdminRole = await context.Roles
            .FirstOrDefaultAsync(x => x.Name == supperAdminRole && x.TenantId == systemTenant.Id);

        if (superAdminRole == null)
        {
            superAdminRole = new ApplicationRole
            {
                Id = Guid.NewGuid(),
                Name = supperAdminRole,
                NormalizedName = supperAdminRole.ToUpperInvariant(),
                Description = "System Super Admin",
                TenantId = systemTenant.Id
            };
            context.Roles.Add(superAdminRole);
            await context.SaveChangesAsync();
        }

        // Ensure User exists
        var adminUser = await context.Users
            .FirstOrDefaultAsync(x => x.UserName == supperAdminName);

        if (adminUser == null)
        {
            adminUser = new ApplicationUser
            {
                Id = Guid.NewGuid(),
                UserName = supperAdminName,
                Email = supperAdminName,
                DisplayName = "Super Admin",
                EmailConfirmed = true,
                IsActive = true,
                SecurityStamp = Guid.NewGuid().ToString(),
                CreatedAt = DateTime.UtcNow,
                NormalizedUserName = supperAdminName.ToUpperInvariant(),
                NormalizedEmail = supperAdminName.ToUpperInvariant()
            };

            var hasher = new PasswordHasher<ApplicationUser>();
            adminUser.PasswordHash = hasher.HashPassword(adminUser, supperAdminPassword);

            context.Users.Add(adminUser);
            await context.SaveChangesAsync();
        }

        // Ensure TenantUser exists
        var tenantUser = await context.TenantUsers
            .FindAsync(adminUser.Id, systemTenant.Id);

        if (tenantUser == null)
        {
            context.TenantUsers.Add(new TenantUser
            {
                UserId = adminUser.Id,
                TenantId = systemTenant.Id,
                RoleId = superAdminRole.Id,
                IsOwner = true,
                CreatedAt = DateTime.UtcNow
            });

            context.UserRoles.Add(new IdentityUserRole<Guid>
            {
                UserId = adminUser.Id,
                RoleId = superAdminRole.Id
            });

            await context.SaveChangesAsync();
        }
    }
}

