using Microsoft.AspNetCore.Identity;

namespace CFW.Identity.Users.Models;

public class ApplicationUser : IdentityUser<Guid>
{
    public ICollection<TenantRole> TenantRoles { get; set; } = new List<TenantRole>();
}