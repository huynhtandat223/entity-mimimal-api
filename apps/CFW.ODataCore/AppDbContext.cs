using CFW.ODataCore.Features.Identity.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using System.Reflection;
using System.Runtime.Loader;

namespace CFW.ODataCore;

public class AppDbContext : IdentityDbContext<ApplicationUser, TenantRole, Guid>
{
    private readonly RuntimeAsmConfig _runtimeAsmConfig;

    public AppDbContext(DbContextOptions<AppDbContext> options, IOptions<RuntimeAsmConfig> runtimeAsmConfig) : base(options)
    {
        _runtimeAsmConfig = runtimeAsmConfig.Value;
    }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        var runtimeEntitiesDir = _runtimeAsmConfig.GetRuntimeEntitiesDirOrDefault();

        var assemblyFiles = Directory.GetFiles(runtimeEntitiesDir, "*.dll", SearchOption.AllDirectories);

        foreach (var path in assemblyFiles)
        {
            Assembly assembly = AssemblyLoadContext.Default.LoadFromAssemblyPath(path);

            var entityTypes = assembly.GetTypes();
            foreach (var entityType in entityTypes)
            {
                builder.Entity(entityType);
            }
        }
    }
}
