using CFW.Core.Results;

namespace CFW.DynamicApi;

public enum OperationHttpMethod
{
    GET, POST, PUT, DELETE, PATCH
}

public class ApiOperationAttribute : Attribute
{
    public string? RouteName { get; set; }

    public OperationHttpMethod HttpMethod { get; set; } = OperationHttpMethod.POST;

    internal Type? TargetType { get; set; }

    public string RouteGroup { get; }

    public ApiOperationAttribute(string routeGroup)
    {
        RouteGroup = routeGroup;
    }
}

public interface IRequestHandler<TRequest, TResponse>
{
    public Task<IResult<TResponse>> Handle(RequestModel<TRequest> request, CancellationToken cancellationToken);
}

public class RequestModel<T>
{
    public IServiceProvider ServiceProvider { get; set; } = null!;

    public T Model { get; set; } = default!;
}