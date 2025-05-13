using System.Text.Json.Serialization;

namespace CFW.AppHost.Infrastructures.IXBrowserGateway.Models;

public class GetOpenedProfilesRequest
{
    // Optional placeholder, Refit requires non-null object
}

public class GetOpenedProfilesResponse
{
    [JsonPropertyName("error")]
    public OpenedProfilesError Error { get; set; } = default!;

    [JsonPropertyName("data")]
    public List<OpenedProfileItem> Data { get; set; } = new();
}

public class OpenedProfilesError
{
    [JsonPropertyName("code")]
    public int Code { get; set; }

    [JsonPropertyName("message")]
    public string Message { get; set; } = default!;

    [JsonPropertyName("time")]
    public long Time { get; set; }
}

public class OpenedProfileItem
{
    [JsonPropertyName("profile_id")]
    public int ProfileId { get; set; }

    [JsonPropertyName("last_opened_user")]
    public string LastOpenedUser { get; set; } = default!;

    [JsonPropertyName("last_opened_time")]
    public string LastOpenedTime { get; set; } = default!;
}
