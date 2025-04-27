using Microsoft.EntityFrameworkCore.Metadata;

namespace CFW.DynamicApi.Deltas;

public class EntityDelta
{
    public IEntityType? EfCoreEntityType { get; set; }

    public IComplexProperty? EfCoreComplexProperty { get; set; }

    public Dictionary<string, object?> ChangedProperties { get; }
        = new Dictionary<string, object?>();

    public virtual object? GetInstance() { return default!; }
}
