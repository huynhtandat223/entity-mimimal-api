using System.Text.Json.Serialization;

namespace CFW.EntityApi.Models;

public class ODataQueryResult<T>
{
    public IEnumerable<T> Value { get; set; } = Array.Empty<T>();

    [JsonPropertyName("@odata.count")]
    public int? TotalCount { set; get; }
}