namespace CFW.AppHost.Infrastructures.IXBrowserGateway.Models;

using System.Text.Json.Serialization;

public class OpenProfileRequest
{
    [JsonPropertyName("profile_id")]
    public int ProfileId { get; set; }

    [JsonPropertyName("args")]
    public List<string>? Args { get; set; }

    [JsonPropertyName("load_extensions")]
    public bool? LoadExtensions { get; set; }

    [JsonPropertyName("load_profile_info_page")]
    public bool? LoadProfileInfoPage { get; set; }

    [JsonPropertyName("cookies_backup")]
    public bool? CookiesBackup { get; set; }

    [JsonPropertyName("cookie")]
    public string? Cookie { get; set; }
}

public class OpenProfileResponse
{
    [JsonPropertyName("error")]
    public OpenProfileError Error { get; set; } = default!;

    [JsonPropertyName("data")]
    public OpenProfileData? Data { get; set; }
}

public class OpenProfileError
{
    [JsonPropertyName("code")]
    public int Code { get; set; }

    [JsonPropertyName("message")]
    public string Message { get; set; } = default!;
}

public class OpenProfileData
{
    [JsonPropertyName("debugging_address")]
    public string DebuggingAddress { get; set; } = default!;

    [JsonPropertyName("debugging_port")]
    public int DebuggingPort { get; set; }

    [JsonPropertyName("profile_id")]
    public int ProfileId { get; set; }

    [JsonPropertyName("pid")]
    public int Pid { get; set; }

    [JsonPropertyName("ws")]
    public string Ws { get; set; } = default!;

    [JsonPropertyName("gateway")]
    public string Gateway { get; set; } = default!;

    [JsonPropertyName("webdriver")]
    public string Webdriver { get; set; } = default!;
}

