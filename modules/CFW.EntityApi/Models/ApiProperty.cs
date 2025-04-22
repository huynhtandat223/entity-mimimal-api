namespace CFW.EntityApi.Models;

public enum ApiPropertyType
{
    Scalar,
    Complex,
    Collection
}

public class ApiProperty
{
    public string Name { get; set; } = string.Empty;

    public Type ClrType { get; set; } = null!;

    public bool IsRequired { get; set; }

    public ApiPropertyType Type { get; set; }

    public IEnumerable<ApiProperty> NestedProperties { get; set; } = Enumerable.Empty<ApiProperty>();
}
