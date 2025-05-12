using System.Text.Json.Serialization;

namespace CFW.AppHost.Infrastructures.IXBrowserGateway.Models;

public class ApiError
{
    [JsonPropertyName("code")]
    public int Code { get; set; }

    [JsonPropertyName("message")]
    public string? Message { get; set; }

    [JsonPropertyName("time")]
    public long Time { get; set; }
}

