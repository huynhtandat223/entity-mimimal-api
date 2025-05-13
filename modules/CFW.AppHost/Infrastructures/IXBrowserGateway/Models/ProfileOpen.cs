namespace CFW.AppHost.Infrastructures.IXBrowserGateway.Models;
public class OpenProfileRequest
{
    public int ProfileId { get; set; }

    public List<string>? Args { get; set; }

    public bool? LoadExtensions { get; set; }

    public bool? LoadProfileInfoPage { get; set; }

    public bool? CookiesBackup { get; set; }

    public string? Cookie { get; set; }
}

public class OpenProfileResponse
{
    public OpenProfileError Error { get; set; } = default!;

    public OpenProfileData? Data { get; set; }
}

public class OpenProfileError
{
    public int Code { get; set; }

    public string Message { get; set; } = default!;
}

public class OpenProfileData
{
    public string DebuggingAddress { get; set; } = default!;

    public int DebuggingPort { get; set; }

    public int ProfileId { get; set; }

    public int Pid { get; set; }

    public string Ws { get; set; } = default!;

    public string Gateway { get; set; } = default!;

    public string Webdriver { get; set; } = default!;
}

