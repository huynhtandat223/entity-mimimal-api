using CFW.AppHost.Features.Core;
using CFW.AppHost.Features.Identity.Models;
using CFW.Core.Dependencies;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace CFW.AppHost.Features.Identity.Services;

public class UserService : IScopedService
{
    private readonly AppDbContext _context;
    private readonly IPasswordHasher<ApplicationUser> _passwordHasher;

    public UserService(AppDbContext context)
    {
        _context = context;
        _passwordHasher = new PasswordHasher<ApplicationUser>();
    }

    public async Task<ApplicationUser> CreateUserAsync(
        string userName,
        string password,
        string displayName,
        string? email = null)
    {
        var user = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            UserName = userName,
            DisplayName = displayName,
            Email = email,
            EmailConfirmed = false,
            PhoneNumberConfirmed = false,
            SecurityStamp = Guid.NewGuid().ToString()
        };

        user.NormalizedUserName = userName.ToUpperInvariant();
        if (!string.IsNullOrEmpty(email))
            user.NormalizedEmail = email.ToUpperInvariant();

        user.PasswordHash = _passwordHasher.HashPassword(user, password);

        _context.Users.Add(user);
        await _context.SaveChangesAsync();
        return user;
    }

    public async Task<ApplicationUser?> UpdateUserAsync(
        Guid userId,
        string? newDisplayName,
        bool? isActive = null)
    {
        var user = await _context.Users.FindAsync(userId);
        if (user == null) return null;

        if (!string.IsNullOrWhiteSpace(newDisplayName))
            user.DisplayName = newDisplayName;

        if (isActive.HasValue)
            user.IsActive = isActive.Value;

        await _context.SaveChangesAsync();
        return user;
    }

    public async Task<bool> DeleteUserAsync(Guid userId)
    {
        var user = await _context.Users.FindAsync(userId);
        if (user == null) return false;

        _context.Users.Remove(user);
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> AddUserToTenantAsync(
        Guid userId,
        Guid tenantId,
        Guid? roleId = null,
        bool isOwner = false)
    {
        var user = await _context.Users.FindAsync(userId);
        var tenant = await _context.Tenants.FindAsync(tenantId);
        if (user == null || tenant == null) return false;

        var existing = await _context.TenantUsers.FindAsync(userId, tenantId);
        if (existing != null)
        {
            existing.RoleId = roleId;
            existing.IsOwner = isOwner;
        }
        else
        {
            _context.TenantUsers.Add(new TenantUser
            {
                UserId = userId,
                TenantId = tenantId,
                RoleId = roleId,
                IsOwner = isOwner
            });
        }

        if (roleId.HasValue)
        {
            var role = await _context.Roles.FindAsync(roleId.Value);
            if (role != null && role.TenantId == tenantId)
            {
                var hasRole = await _context.UserRoles
                    .AnyAsync(r => r.UserId == userId && r.RoleId == roleId);

                if (!hasRole)
                {
                    _context.UserRoles.Add(new IdentityUserRole<Guid>
                    {
                        UserId = userId,
                        RoleId = roleId.Value
                    });
                }
            }
        }

        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<List<ApplicationUser>> GetUsersByTenantAsync(Guid tenantId)
    {
        return await _context.TenantUsers
            .Where(tu => tu.TenantId == tenantId)
            .Include(tu => tu.User)
            .Select(tu => tu.User!)
            .ToListAsync();
    }

    public async Task<bool> AssignRoleToUserAsync(Guid userId, Guid roleId)
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
}