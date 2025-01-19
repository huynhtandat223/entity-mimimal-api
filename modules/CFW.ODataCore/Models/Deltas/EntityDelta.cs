using Microsoft.EntityFrameworkCore.Metadata;

namespace CFW.ODataCore.Models.Deltas;

public class EntityDelta
{
    public IEntityType? EfCoreEntityType { get; set; }

    public IComplexProperty? ComplexProperty { get; set; }

    public Dictionary<string, object?> ChangedProperties { get; }
        = new Dictionary<string, object?>();

    public virtual object? GetInstance() { return default!; }
}
