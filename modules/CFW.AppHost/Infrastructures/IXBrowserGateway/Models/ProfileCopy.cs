namespace CFW.AppHost.Infrastructures.IXBrowserGateway.Models;

using System.Text.Json.Serialization;

public class CopyProfileRequest
{
    [JsonPropertyName("profile_id")]
    public int ProfileId { get; set; }

    [JsonPropertyName("site_id")]
    public int? SiteId { get; set; }

    [JsonPropertyName("site_url")]
    public string? SiteUrl { get; set; }

    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("group_id")]
    public int? GroupId { get; set; }
}

public class CopyProfileResponse
{
    [JsonPropertyName("error")]
    public ApiError Error { get; set; } = default!;

    [JsonPropertyName("data")]
    public int Data { get; set; }
}
