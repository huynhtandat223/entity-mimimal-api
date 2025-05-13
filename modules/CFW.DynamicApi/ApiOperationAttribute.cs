using CFW.Core.Results;

namespace CFW.DynamicApi;

public enum OperationHttpMethod
{
    GET, POST, PUT, DELETE, PATCH
}

public class ApiOperationAttribute : Attribute
{
    public string? RouteName { get; set; }

    public OperationHttpMethod HttpMethod { get; } = OperationHttpMethod.POST;

    internal Type? TargetType { get; set; }

    public string RouteGroup { get; }

    public ApiOperationAttribute(string routeGroup)
    {
        RouteGroup = routeGroup;
    }
}

public interface IApiOperationHandler<TRequest, TResponse>
{
    public Task<IResult<TResponse>> Handle(TRequest request, CancellationToken cancellationToken);
}

public interface IApiOperationHandler<TResponse>
{
    public Task<TResponse> Handle(CancellationToken cancellationToken);
}