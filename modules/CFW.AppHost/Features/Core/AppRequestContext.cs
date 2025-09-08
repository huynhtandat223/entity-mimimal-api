using CFW.AppHost.Utils;
using CFW.Core.Dependencies;
using Microsoft.EntityFrameworkCore;

namespace CFW.AppHost.Features.Core;

public class AppRequestContext : IScopedService
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public AppRequestContext(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public async Task<DbContext> GetOrCreateDbContext(CancellationToken cancellationToken = default)
    {
        await Task.CompletedTask;

        if (_httpContextAccessor.HttpContext == null)
            return default!;

        var tenantId = TenantId;
        if (tenantId.IsNullOrEmpty())
            return default!;

        return _httpContextAccessor.HttpContext.RequestServices.GetRequiredService<AppDbContext>();
    }

    public Guid? TenantId
        => _httpContextAccessor.HttpContext?.User.FindFirst("tenantId")?.Value?.ToGuid();

    public IEnumerable<string> TenantIds
        => _httpContextAccessor.HttpContext?.User
               .FindAll("tenant")
               .Select(c => c.Value)
           ?? Enumerable.Empty<string>();
}
