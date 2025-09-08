using CFW.AppHost.Features.Core;
using CFW.AppHost.Features.Identity.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using System.Security.Claims;

namespace CFW.AppHost.Features.Identity.Configurations;

public class ApplicationUserClaimsPrincipalFactory
    : UserClaimsPrincipalFactory<ApplicationUser, ApplicationRole>
{
    private readonly AppDbContext _db;

    public ApplicationUserClaimsPrincipalFactory(
        UserManager<ApplicationUser> userManager,
        RoleManager<ApplicationRole> roleManager,
        IOptions<IdentityOptions> optionsAccessor, AppDbContext db)
        : base(userManager, roleManager, optionsAccessor)
    {
        _db = db;
    }

    protected override async Task<ClaimsIdentity> GenerateClaimsAsync(ApplicationUser user)
    {
        var identity = await base.GenerateClaimsAsync(user);

        var tenantIds = await _db.TenantUsers
            .AsNoTracking()
            .Where(tu => tu.UserId == user.Id)
            .Select(tu => tu.TenantId)
            .ToListAsync();

        if (tenantIds.Any())
        {
            foreach (var tid in tenantIds)
                identity.AddClaim(new Claim("tenant", tid.ToString()));
            identity.AddClaim(new Claim("tenantId", tenantIds.First().ToString()));
        }

        return identity;
    }
}

