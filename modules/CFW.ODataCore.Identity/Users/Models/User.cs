using CFW.ODataCore.Identity.Tenants.Models;
using Microsoft.AspNetCore.Identity;

namespace CFW.ODataCore.Identity.Users.Models;

public class User : IdentityUser<Guid>
{
    public ICollection<Tenant>? Tenants { get; set; }
}
