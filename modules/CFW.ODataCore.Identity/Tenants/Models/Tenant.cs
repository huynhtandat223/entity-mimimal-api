using CFW.Core.Entities;
using CFW.ODataCore.Identity.Users.Models;

namespace CFW.ODataCore.Identity.Tenants.Models;

public class Tenant : IEntity<Guid>
{
    public Guid Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string? DisplayName { get; set; }

    public ICollection<User>? Users { get; set; }
}
