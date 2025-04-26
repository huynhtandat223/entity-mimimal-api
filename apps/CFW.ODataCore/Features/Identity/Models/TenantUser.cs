namespace CFW.ODataCore.Features.Identity.Models;

public class TenantUser
{
    public Guid UserId { get; set; }

    public Guid TenantId { get; set; }

    public Guid? RoleId { get; set; }

    public bool IsOwner { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual ApplicationUser? User { get; set; }

    public virtual Tenant? Tenant { get; set; }

    public virtual ApplicationRole? Role { get; set; }

    public TenantUser()
    {
        CreatedAt = DateTime.UtcNow;
        IsOwner = false;
    }
}
