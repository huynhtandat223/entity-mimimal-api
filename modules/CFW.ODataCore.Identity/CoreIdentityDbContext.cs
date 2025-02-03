using CFW.ODataCore.Identity.Users.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace CFW.ODataCore.Identity;

public class CoreIdentityDbContext : IdentityDbContext<ApplicationUser, TenantRole, Guid>
{
    public CoreIdentityDbContext(DbContextOptions<CoreIdentityDbContext> options) : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
    }
}
