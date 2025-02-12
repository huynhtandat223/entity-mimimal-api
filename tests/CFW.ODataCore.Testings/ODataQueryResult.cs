using System.Text.Json.Serialization;

namespace CFW.EntityMinimalApi.Testings;

public class ODataQueryResult<TViewModel>
{
    public IEnumerable<TViewModel> Value { set; get; } = Enumerable.Empty<TViewModel>();

    [JsonPropertyName("@odata.count")]
    public int? TotalCount { set; get; }
}
