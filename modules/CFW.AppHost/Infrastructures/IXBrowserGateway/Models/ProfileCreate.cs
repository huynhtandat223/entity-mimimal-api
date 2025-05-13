namespace CFW.AppHost.Infrastructures.IXBrowserGateway.Models;

using System.Collections.Generic;
using System.Text.Json.Serialization;

public class ProxyConfig
{
    [JsonPropertyName("proxy_mode")]
    public int ProxyMode { get; set; } = 2;

    [JsonPropertyName("proxy_check_line")]
    public string ProxyCheckLine { get; set; } = "global_line";

    [JsonPropertyName("proxy_id")]
    public string? ProxyId { get; set; }

    [JsonPropertyName("proxy_type")]
    public string ProxyType { get; set; } = "direct";

    [JsonPropertyName("proxy_ip")]
    public string? ProxyIp { get; set; }

    [JsonPropertyName("proxy_port")]
    public string? ProxyPort { get; set; }

    [JsonPropertyName("proxy_user")]
    public string? ProxyUser { get; set; }

    [JsonPropertyName("proxy_password")]
    public string? ProxyPassword { get; set; }

    [JsonPropertyName("ip_detection")]
    public string IpDetection { get; set; } = "0";

    [JsonPropertyName("traffic_package_ip_policy")]
    public bool TrafficPackageIpPolicy { get; set; } = false;

    [JsonPropertyName("proxy_service")]
    public string ProxyService { get; set; } = "general";

    [JsonPropertyName("proxy_data_format_type")]
    public string ProxyDataFormatType { get; set; } = "txt";

    [JsonPropertyName("proxy_data_txt_format")]
    public string ProxyDataTxtFormat { get; set; } = "ip:port";

    [JsonPropertyName("proxy_data_json_format")]
    public Dictionary<string, string> ProxyDataJsonFormat { get; set; } = new()
    {
        ["ip"] = "ip",
        ["port"] = "port",
        ["username"] = "username",
        ["password"] = "password"
    };

    [JsonPropertyName("proxy_extraction_method")]
    public string ProxyExtractionMethod { get; set; } = "invalid";

    [JsonPropertyName("proxy_url")]
    public string? ProxyUrl { get; set; }

    [JsonPropertyName("use_system_proxy")]
    public string UseSystemProxy { get; set; } = "1";

    [JsonPropertyName("enable_bypass")]
    public string EnableBypass { get; set; } = "0";

    [JsonPropertyName("bypass_list")]
    public string? BypassList { get; set; }
}

public class CreateProfileRequest
{
    [JsonPropertyName("site_id")]
    public int SiteId { get; set; } = 21;

    [JsonPropertyName("site_url")]
    public string SiteUrl { get; set; } = "http://baidu.com/";

    [JsonPropertyName("color")]
    public string Color { get; set; } = "#CC9966";

    [JsonPropertyName("name")]
    public string Name { get; set; } = default!;

    [JsonPropertyName("note")]
    public string? Note { get; set; }

    [JsonPropertyName("group_id")]
    public int GroupId { get; set; } = 1;

    [JsonPropertyName("tag")]
    public string? Tag { get; set; }

    [JsonPropertyName("username")]
    public string? Username { get; set; }

    [JsonPropertyName("password")]
    public string? Password { get; set; }

    [JsonPropertyName("tfa_secret")]
    public string? TfaSecret { get; set; }

    [JsonPropertyName("cookie")]
    public string? Cookie { get; set; }

    [JsonPropertyName("proxy_config")]
    public ProxyConfig? ProxyConfig { get; set; }

    // You can add FingerprintConfig and PreferenceConfig here similarly
}

public class CreateProfileResponse
{
    [JsonPropertyName("error")]
    public CreateProfileError Error { get; set; } = default!;

    [JsonPropertyName("data")]
    public int Data { get; set; }
}

public class CreateProfileError
{
    [JsonPropertyName("code")]
    public int Code { get; set; }

    [JsonPropertyName("message")]
    public string Message { get; set; } = default!;

    [JsonPropertyName("time")]
    public long Time { get; set; }
}

