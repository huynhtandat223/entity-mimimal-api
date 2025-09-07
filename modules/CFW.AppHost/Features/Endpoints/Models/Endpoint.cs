using CFW.Core.Entities;

namespace CFW.AppHost.Features.Endpoints.Models;

public class Endpoint : IEntity<Guid>
{
    public Guid Id { set; get; }

    public string Path { set; get; } = string.Empty;

    public HttpMethod Method { set; get; } = HttpMethod.POST;

    public string? Description { set; get; }

    public bool IsAuthenticationRequired { set; get; }

    public EndpointAuthorization? Authorization { set; get; }

    public EndpointODataSupportOptions? ODataOptions { set; get; }

    public RuntimeEntityDefinition? RuntimeEntityDefinition { set; get; }

    public ContainerConfiguration ContainerConfiguration { set; get; } = default!;
}

public enum HttpMethod
{
    GET,
    POST,
    PUT,
    DELETE,
    PATCH,
    OPTIONS,
    HEAD
}

public class EndpointAuthorization : IEntity<Guid>
{
    public Guid Id { set; get; }

    public string? Roles { set; get; }

    public string? Permissions { set; get; }

    public string? Policies { set; get; }
}

public class EndpointODataSupportOptions : IEntity<Guid>
{
    public Guid Id { set; get; }

    public int? MaxTop { set; get; }

    public bool AllowFilter { set; get; } = true;

    public bool AllowOrderBy { set; get; } = true;
}
