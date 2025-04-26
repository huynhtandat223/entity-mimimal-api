using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

namespace CFW.DynamicApi;

public class DynamicOperationBuilder
{
    private readonly DynamicApiOperation _operation = new();

    public DynamicOperationBuilder WithMethod(string httpMethod)
    {
        _operation.HttpMethod = httpMethod;
        return this;
    }

    public DynamicOperationBuilder WithRoute(string route)
    {
        _operation.Route = route;
        return this;
    }

    public DynamicOperationBuilder WithRequest<TRequest>()
    {
        _operation.RequestType = typeof(TRequest);
        return this;
    }

    public DynamicOperationBuilder WithResponse<TResponse>()
    {
        _operation.ResponseType = typeof(TResponse);
        return this;
    }

    public DynamicOperationBuilder Handle(Func<HttpContext, Task<object?>> handler)
    {
        _operation.Handler = handler;
        return this;
    }

    public DynamicOperationBuilder ConfigureRouteGroup(Action<RouteGroupBuilder> configure)
    {
        _operation.ConfigureRoute += configure;
        return this;
    }

    public DynamicOperationBuilder AddInterceptor<TInterceptor>()
        where TInterceptor : class, IOperationInterceptor
    {
        _operation.InterceptorFactories.Add(sp => sp.GetRequiredService<TInterceptor>());
        return this;
    }

    public DynamicApiOperation Build() => _operation;
}
