using Microsoft.AspNetCore.OData.Query;

namespace CFW.DynamicApi;

public class ContainerConfiguration
{
    public string RoutePrefix { get; set; } = string.Empty;
    public int DefaultPageSize { get; set; } = 50;
    public AllowedQueryOptions AllowedQueryOptions { get; set; } = AllowedQueryOptions.All;
}
