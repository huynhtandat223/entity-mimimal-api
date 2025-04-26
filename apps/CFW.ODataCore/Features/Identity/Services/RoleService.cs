using CFW.Core.Dependencies;
using CFW.ODataCore.Features.Identity.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace CFW.ODataCore.Features.Identity.Services;

public class RoleService : IScopedService
{
    private readonly AppDbContext _context;

    public RoleService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<ApplicationRole?> CreateRoleAsync(
        string roleName,
        string description,
        Guid tenantId)
    {
        var tenant = await _context.Tenants.FindAsync(tenantId);
        if (tenant == null) return null;

        var role = new ApplicationRole
        {
            Id = Guid.NewGuid(),
            Name = roleName,
            NormalizedName = roleName.ToUpperInvariant(),
            Description = description,
            TenantId = tenantId
        };

        _context.Roles.Add(role);
        await _context.SaveChangesAsync();
        return role;
    }

    public async Task<ApplicationRole?> UpdateRoleAsync(Guid roleId, string? newDescription)
    {
        var role = await _context.Roles.FindAsync(roleId);
        if (role == null) return null;

        if (!string.IsNullOrWhiteSpace(newDescription))
            role.Description = newDescription;

        await _context.SaveChangesAsync();
        return role;
    }

    public async Task<bool> DeleteRoleAsync(Guid roleId)
    {
        var role = await _context.Roles.FindAsync(roleId);
        if (role == null) return false;

        _context.Roles.Remove(role);
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> AssignRoleToUserAsync(Guid roleId, Guid userId)
    {
        var user = await _context.Users.FindAsync(userId);
        var role = await _context.Roles.FindAsync(roleId);
        if (user == null || role == null) return false;

        var membership = await _context.TenantUsers.FindAsync(userId, role.TenantId);
        if (membership == null)
        {
            _context.TenantUsers.Add(new TenantUser
            {
                UserId = userId,
                TenantId = role.TenantId,
                RoleId = roleId
            });
        }
        else
        {
            membership.RoleId = roleId;
        }

        if (!await _context.UserRoles.AnyAsync(r => r.UserId == userId && r.RoleId == roleId))
        {
            _context.UserRoles.Add(new IdentityUserRole<Guid>
            {
                UserId = userId,
                RoleId = roleId
            });
        }

        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<List<ApplicationRole>> GetRolesByTenantAsync(Guid tenantId)
    {
        return await _context.Roles
            .Where(r => r.TenantId == tenantId)
            .ToListAsync();
    }
}
