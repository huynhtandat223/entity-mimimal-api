namespace CFW.DynamicApi;

public class PropertyMetadata
{
    public required string Name { set; get; }

    public required Type ClrType { set; get; }

    public required bool IsKey { set; get; }

    public required bool IsRequired { set; get; }

    public PropertyType PropertyType { set; get; } = PropertyType.Scalar;

    public IEnumerable<PropertyMetadata>? ChildProperties { set; get; }
}
