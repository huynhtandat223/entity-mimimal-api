using System.Text.Json.Serialization;

namespace CFW.AppHost.Infrastructures.IXBrowserGateway.Models;

public class ProfileListRequest
{
    [JsonPropertyName("profile_id")]
    public int? ProfileId { get; set; } = 0;

    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("group_id")]
    public int? GroupId { get; set; } = 0;

    [JsonPropertyName("tag_id")]
    public int? TagId { get; set; } = 0;

    [JsonPropertyName("page")]
    public int Page { get; set; } = 1;

    [JsonPropertyName("limit")]
    public int Limit { get; set; } = 10;
}

public class ProfileListResponse
{
    [JsonPropertyName("error")]
    public ApiError? Error { get; set; }

    [JsonPropertyName("data")]
    public ProfileListData? Data { get; set; }
}

public class ProfileListData
{
    [JsonPropertyName("total")]
    public int Total { get; set; }

    [JsonPropertyName("data")]
    public List<ProfileInfo> Profiles { get; set; } = new();
}

public class ProfileInfo
{
    [JsonPropertyName("profile_id")]
    public int ProfileId { get; set; }

    [JsonPropertyName("site_url")]
    public string? SiteUrl { get; set; }

    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("note")]
    public string? Note { get; set; }

    [JsonPropertyName("color")]
    public string? Color { get; set; }

    [JsonPropertyName("username")]
    public string? Username { get; set; }

    [JsonPropertyName("password")]
    public string? Password { get; set; }

    [JsonPropertyName("last_open_time")]
    public long LastOpenTime { get; set; }

    [JsonPropertyName("group_id")]
    public int GroupId { get; set; }

    [JsonPropertyName("group_name")]
    public string? GroupName { get; set; }

    [JsonPropertyName("tag_id")]
    public string? TagId { get; set; }

    [JsonPropertyName("tag_name")]
    public string? TagName { get; set; }

    [JsonPropertyName("proxy_mode")]
    public int ProxyMode { get; set; }

    [JsonPropertyName("proxy_id")]
    public int? ProxyId { get; set; }

    [JsonPropertyName("proxy_type")]
    public string? ProxyType { get; set; }

    [JsonPropertyName("proxy_ip")]
    public string? ProxyIp { get; set; }

    [JsonPropertyName("proxy_port")]
    public string? ProxyPort { get; set; }

    [JsonPropertyName("real_ip")]
    public string? RealIp { get; set; }

    [JsonPropertyName("cache_path")]
    public string? CachePath { get; set; }
}

