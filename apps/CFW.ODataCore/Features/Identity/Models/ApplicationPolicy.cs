namespace CFW.ODataCore.Features.Identity.Models;

public class ApplicationPolicy
{
    public Guid Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }

    public Guid TenantId { get; set; }

    public virtual Tenant? Tenant { get; set; }
}
