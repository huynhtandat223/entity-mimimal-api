using Microsoft.EntityFrameworkCore.Metadata;

namespace CFW.ODataCore.Models.Metadata;

public class MetadataEntityProperty
{
    public IEntityType? EfCoreEntityType { set; get; }

    public IComplexProperty? EfCoreComplexProperty { set; get; }

    public IEnumerable<IProperty> ScalarProperties { set; get; } = Enumerable.Empty<IProperty>();

    public IEnumerable<INavigation> CollectionProperties { set; get; } = Enumerable.Empty<INavigation>();

    public IEnumerable<INavigation> EntityProperties { set; get; } = Enumerable.Empty<INavigation>();

    public IEnumerable<IComplexProperty> ComplexProperties { set; get; } = Enumerable.Empty<IComplexProperty>();
}
