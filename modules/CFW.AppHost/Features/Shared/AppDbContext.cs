using CFW.AppHost.Features.Identity.Models;
using CFW.Core.Entities;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace CFW.AppHost.Features.Shared;

public class AppDbContext : IdentityDbContext<ApplicationUser, ApplicationRole, Guid>
{
    public AppDbContext(DbContextOptions<AppDbContext> options, IEnumerable<Type> runtimeTypes) : base(options)
    {
    }

    public DbSet<Tenant> Tenants { get; set; }

    public DbSet<ApplicationPolicy> Policies { get; set; }

    public DbSet<TenantUser> TenantUsers { get; set; }

    public DbSet<UserPolicy> UserPolicies { get; set; }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        //scan current domain for entities that use marker interface
        var markerType = typeof(IEntity<>);
        var scanedEntityTypes = AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(assembly => assembly.GetExportedTypes())
                .Where(type => type.GetInterfaces()
                    .Any(i => markerType == i || i.IsGenericType && markerType == i.GetGenericTypeDefinition()))
                .ToArray();
        foreach (var entityType in scanedEntityTypes)
        {
            builder.Entity(entityType);
        }


        // Configure identity entities
        builder.Entity<TenantUser>().HasKey(tu => new { tu.UserId, tu.TenantId });
        builder.Entity<UserPolicy>().HasKey(up => new { up.UserId, up.PolicyId });

        builder.Entity<ApplicationRole>()
               .HasOne(r => r.Tenant)
               .WithMany(t => t.Roles)
               .HasForeignKey(r => r.TenantId)
               .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<ApplicationPolicy>()
               .HasOne(p => p.Tenant)
               .WithMany(t => t.Policies)
               .HasForeignKey(p => p.TenantId)
               .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<TenantUser>()
               .HasOne(tu => tu.User)
               .WithMany(u => u.TenantUsers)
               .HasForeignKey(tu => tu.UserId)
               .OnDelete(DeleteBehavior.Cascade);
        builder.Entity<TenantUser>()
               .HasOne(tu => tu.Tenant)
               .WithMany(t => t.TenantUsers)
               .HasForeignKey(tu => tu.TenantId)
               .OnDelete(DeleteBehavior.Restrict);
        builder.Entity<TenantUser>()
               .HasOne(tu => tu.Role)
               .WithMany()
               .HasForeignKey(tu => tu.RoleId)
               .OnDelete(DeleteBehavior.SetNull);

        builder.Entity<UserPolicy>()
               .HasOne(up => up.User)
               .WithMany(u => u.UserPolicies)
               .HasForeignKey(up => up.UserId)
               .OnDelete(DeleteBehavior.Cascade);
        builder.Entity<UserPolicy>()
               .HasOne(up => up.Policy)
               .WithMany()
               .HasForeignKey(up => up.PolicyId)
               .OnDelete(DeleteBehavior.Cascade);
    }
}
