using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace CFW.DynamicApi;

public class DynamicApiOperation
{
    public string HttpMethod { get; set; } = "GET";
    public string Route { get; set; } = "/";
    public Type? RequestType { get; set; }
    public Type? ResponseType { get; set; }
    public Func<HttpContext, Task<object?>>? Handler { get; set; }
    public Action<RouteGroupBuilder>? ConfigureRoute { get; set; }
    public List<Func<IServiceProvider, IOperationInterceptor>> InterceptorFactories { get; } = new();
}
