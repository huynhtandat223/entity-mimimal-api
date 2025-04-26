using Microsoft.AspNetCore.Identity;

namespace CFW.AppHost.Features.Identity.Models;

public class ApplicationUser : IdentityUser<Guid>
{
    public string DisplayName { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; }

    public bool IsActive { get; set; }

    public virtual ICollection<TenantUser> TenantUsers { get; set; } = new List<TenantUser>();

    public virtual ICollection<UserPolicy> UserPolicies { get; set; } = new List<UserPolicy>();

    public ApplicationUser()
    {
        CreatedAt = DateTime.UtcNow;
        IsActive = true;
    }
}
