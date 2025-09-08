using CFW.AppHost.Features.Core;
using CFW.AppHost.Features.Identity.Models;
using CFW.Core.Dependencies;
using Microsoft.EntityFrameworkCore;

namespace CFW.AppHost.Features.Identity.Services;

public class PolicyService : IScopedService
{
    private readonly AppDbContext _db;

    public PolicyService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<ApplicationPolicy?> CreatePolicyAsync(
        string name,
        string? description,
        Guid tenantId)
    {
        var tenant = await _db.Tenants.FindAsync(tenantId);
        if (tenant == null) return null;

        var policy = new ApplicationPolicy
        {
            Id = Guid.NewGuid(),
            Name = name,
            Description = description,
            TenantId = tenantId
        };

        _db.Policies.Add(policy);
        await _db.SaveChangesAsync();
        return policy;
    }

    public async Task<ApplicationPolicy?> UpdatePolicyAsync(
        Guid policyId,
        string? newName,
        string? newDescription)
    {
        var policy = await _db.Policies.FindAsync(policyId);
        if (policy == null) return null;

        if (!string.IsNullOrWhiteSpace(newName))
            policy.Name = newName;

        if (!string.IsNullOrWhiteSpace(newDescription))
            policy.Description = newDescription;

        await _db.SaveChangesAsync();
        return policy;
    }

    public async Task<bool> DeletePolicyAsync(Guid policyId)
    {
        var policy = await _db.Policies.FindAsync(policyId);
        if (policy == null) return false;

        _db.Policies.Remove(policy);
        await _db.SaveChangesAsync();
        return true;
    }

    public async Task<bool> AssignPolicyToUserAsync(Guid userId, Guid policyId)
    {
        var user = await _db.Users.FindAsync(userId);
        var policy = await _db.Policies.FindAsync(policyId);
        if (user == null || policy == null) return false;

        if (await _db.TenantUsers.FindAsync(userId, policy.TenantId) == null)
        {
            _db.TenantUsers.Add(new TenantUser
            {
                UserId = userId,
                TenantId = policy.TenantId,
                IsOwner = false
            });
        }

        var exists = await _db.UserPolicies
            .AnyAsync(x => x.UserId == userId && x.PolicyId == policyId);

        if (!exists)
        {
            _db.UserPolicies.Add(new UserPolicy
            {
                UserId = userId,
                PolicyId = policyId
            });
        }

        await _db.SaveChangesAsync();
        return true;
    }

    public async Task<List<ApplicationPolicy>> GetUserPoliciesAsync(Guid userId, Guid tenantId)
    {
        return await _db.UserPolicies
            .Where(up => up.UserId == userId && up.Policy!.TenantId == tenantId)
            .Select(up => up.Policy!)
            .ToListAsync();
    }
}
