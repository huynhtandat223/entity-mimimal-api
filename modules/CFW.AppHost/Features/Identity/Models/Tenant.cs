namespace CFW.AppHost.Features.Identity.Models;

public class Tenant
{
    public Guid Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string ConnectionString { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public virtual ICollection<ApplicationRole> Roles { get; set; } = new List<ApplicationRole>();

    public virtual ICollection<ApplicationPolicy> Policies { get; set; } = new List<ApplicationPolicy>();

    public virtual ICollection<TenantUser> TenantUsers { get; set; } = new List<TenantUser>();
}
