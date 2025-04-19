using Microsoft.AspNetCore.Identity;

namespace CFW.ODataCore.Features.Identity.Models;

public class ApplicationUser : IdentityUser<Guid>
{
    public ICollection<TenantRole> TenantRoles { get; set; } = new List<TenantRole>();
}