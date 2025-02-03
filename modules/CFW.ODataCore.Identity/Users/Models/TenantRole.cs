using Microsoft.AspNetCore.Identity;

namespace CFW.ODataCore.Identity.Users.Models;

public class TenantRole : IdentityRole<Guid>
{
    public Guid TenantId { get; set; } = default!;

    public TenantRole(Guid tenantId, string name) : base(name)
    {
        TenantId = tenantId;
    }
}
