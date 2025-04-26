using CFW.Core.Dependencies;
using CFW.ODataCore.Features.Identity.Models;
using Microsoft.EntityFrameworkCore;

namespace CFW.ODataCore.Features.Identity.Services;

public class PolicyService : IScopedService
{
    private readonly AppDbContext _context;

    public PolicyService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<ApplicationPolicy?> CreatePolicyAsync(
        string name,
        string? description,
        Guid tenantId)
    {
        var tenant = await _context.Tenants.FindAsync(tenantId);
        if (tenant == null) return null;

        var policy = new ApplicationPolicy
        {
            Id = Guid.NewGuid(),
            Name = name,
            Description = description,
            TenantId = tenantId
        };

        _context.Policies.Add(policy);
        await _context.SaveChangesAsync();
        return policy;
    }

    public async Task<ApplicationPolicy?> UpdatePolicyAsync(
        Guid policyId,
        string? newName,
        string? newDescription)
    {
        var policy = await _context.Policies.FindAsync(policyId);
        if (policy == null) return null;

        if (!string.IsNullOrWhiteSpace(newName))
            policy.Name = newName;

        if (!string.IsNullOrWhiteSpace(newDescription))
            policy.Description = newDescription;

        await _context.SaveChangesAsync();
        return policy;
    }

    public async Task<bool> DeletePolicyAsync(Guid policyId)
    {
        var policy = await _context.Policies.FindAsync(policyId);
        if (policy == null) return false;

        _context.Policies.Remove(policy);
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> AssignPolicyToUserAsync(Guid userId, Guid policyId)
    {
        var user = await _context.Users.FindAsync(userId);
        var policy = await _context.Policies.FindAsync(policyId);
        if (user == null || policy == null) return false;

        if (await _context.TenantUsers.FindAsync(userId, policy.TenantId) == null)
        {
            _context.TenantUsers.Add(new TenantUser
            {
                UserId = userId,
                TenantId = policy.TenantId,
                IsOwner = false
            });
        }

        var exists = await _context.UserPolicies
            .AnyAsync(x => x.UserId == userId && x.PolicyId == policyId);

        if (!exists)
        {
            _context.UserPolicies.Add(new UserPolicy
            {
                UserId = userId,
                PolicyId = policyId
            });
        }

        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<List<ApplicationPolicy>> GetUserPoliciesAsync(Guid userId, Guid tenantId)
    {
        return await _context.UserPolicies
            .Where(up => up.UserId == userId && up.Policy!.TenantId == tenantId)
            .Select(up => up.Policy!)
            .ToListAsync();
    }
}
