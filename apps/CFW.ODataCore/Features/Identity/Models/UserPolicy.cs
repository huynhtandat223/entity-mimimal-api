namespace CFW.ODataCore.Features.Identity.Models;

public class UserPolicy
{
    public Guid UserId { get; set; }

    public Guid PolicyId { get; set; }

    public virtual ApplicationUser? User { get; set; }

    public virtual ApplicationPolicy? Policy { get; set; }
}