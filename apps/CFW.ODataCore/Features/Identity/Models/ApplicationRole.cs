using Microsoft.AspNetCore.Identity;

namespace CFW.ODataCore.Features.Identity.Models;

public class ApplicationRole : IdentityRole<Guid>
{
    public string Description { get; set; } = string.Empty;

    public Guid TenantId { get; set; }

    public virtual Tenant? Tenant { get; set; }
}
