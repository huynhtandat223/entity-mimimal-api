using CFW.Core.Entities;
using CFW.ODataCore.Identity.Users.Models;

namespace CFW.ODataCore.Identity.Tenants.Models;

public class Tenant : IEntity<Guid>
{
    public Guid Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string? DisplayName { get; set; }

    public TenantType Type { get; set; }

    public ICollection<ApplicationUser>? Users { get; set; }
}

public enum TenantType : byte
{
    Organization = 0,
    Personal = 1,
    System = 2
}
