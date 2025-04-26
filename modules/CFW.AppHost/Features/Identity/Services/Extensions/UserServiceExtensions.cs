using CFW.AppHost.Features.Identity.Models;
using CFW.AppHost.Features.Shared;
using Microsoft.EntityFrameworkCore;

namespace CFW.AppHost.Features.Identity.Services.Extensions;

public static class UserServiceExtensions
{
    public static async Task<List<Tenant>> GetTenantsOfUserAsync(this AppDbContext context, Guid userId)
    {
        return await context.TenantUsers
            .Where(tu => tu.UserId == userId)
            .Select(tu => tu.Tenant!)
            .ToListAsync();
    }

    public static async Task<bool> IsUserInTenantAsync(this AppDbContext context, Guid userId, Guid tenantId)
    {
        return await context.TenantUsers.AnyAsync(tu => tu.UserId == userId && tu.TenantId == tenantId);
    }
}
