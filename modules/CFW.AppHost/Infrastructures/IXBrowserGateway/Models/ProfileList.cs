namespace CFW.AppHost.Infrastructures.IXBrowserGateway.Models;

public class ProfileListRequest
{
    public int? ProfileId { get; set; } = 0;

    public string? Name { get; set; }

    public int? GroupId { get; set; } = 0;

    public int? TagId { get; set; } = 0;

    public int Page { get; set; } = 1;

    public int Limit { get; set; } = 10;
}

public class ProfileListResponse
{
    public ApiError? Error { get; set; }

    public ProfileListData? Data { get; set; }
}

public class ProfileListData
{
    public int Total { get; set; }

    public List<ProfileInfo> Data { get; set; } = new();
}

public class ProfileInfo
{
    public int ProfileId { get; set; }

    public string? SiteUrl { get; set; }

    public string? Name { get; set; }

    public string? Note { get; set; }

    public string? Color { get; set; }

    public string? Username { get; set; }

    public string? Password { get; set; }

    public long LastOpenTime { get; set; }

    public int GroupId { get; set; }

    public string? GroupName { get; set; }

    public string? TagId { get; set; }

    public string? TagName { get; set; }

    public int ProxyMode { get; set; }

    public int? ProxyId { get; set; }

    public string? ProxyType { get; set; }

    public string? ProxyIp { get; set; }

    public string? ProxyPort { get; set; }

    public string? RealIp { get; set; }

    public string? CachePath { get; set; }
}

