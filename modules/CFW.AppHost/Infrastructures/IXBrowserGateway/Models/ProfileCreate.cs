namespace CFW.AppHost.Infrastructures.IXBrowserGateway.Models;

using System.Collections.Generic;
using System.Text.Json.Serialization;

public class ProxyConfig
{
    public int ProxyMode { get; set; } = 2;

    public string ProxyCheckLine { get; set; } = "global_line";

    public string? ProxyId { get; set; }

    public string ProxyType { get; set; } = "direct";

    public string? ProxyIp { get; set; }

    public string? ProxyPort { get; set; }

    public string? ProxyUser { get; set; }

    public string? ProxyPassword { get; set; }

    public string IpDetection { get; set; } = "0";

    public bool TrafficPackageIpPolicy { get; set; } = false;

    public string ProxyService { get; set; } = "general";

    public string ProxyDataFormatType { get; set; } = "txt";

    public string ProxyDataTxtFormat { get; set; } = "ip:port";

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
    public int? SiteId { get; set; }

    public string SiteUrl { get; set; } = string.Empty;

    public string? Color { get; set; } //= "#CC9966";

    public string Name { get; set; } = default!;

    public string? Note { get; set; }

    public int GroupId { get; set; } = 1;

    public string? Tag { get; set; }

    public string? Username { get; set; }

    public string? Password { get; set; }

    public string? TfaSecret { get; set; }

    public string? Cookie { get; set; }

    public ProxyConfig? ProxyConfig { get; set; }
}

public class CreateProfileResponse
{
    public CreateProfileError Error { get; set; } = default!;

    public int Data { get; set; }
}

public class CreateProfileError
{
    public int Code { get; set; }

    public string Message { get; set; } = default!;

    public long Time { get; set; }
}

