using CFW.DynamicApi.Buiders;
using CFW.DynamicApi.Interceptors;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

namespace CFW.DynamicApi;

public enum PropertyType
{
    Scalar, Complex, Collection
}

public class PropertyMetadata
{
    public required string Name { set; get; }

    public required Type ClrType { set; get; }

    public required bool IsKey { set; get; }

    public required bool IsRequired { set; get; }

    public PropertyType PropertyType { set; get; } = PropertyType.Scalar;

    public IEnumerable<PropertyMetadata>? ChildProperties { set; get; }
}

public class DynamicApiOperation
{
    public string HttpMethod { get; set; } = "GET";

    public string Route { get; set; } = "/";

    public Type? RequestType { get; set; }

    public Type? ResponseType { get; set; }

    public Func<HttpContext, Task<object?>>? Handler { get; set; }

    public Action<RouteGroupBuilder>? ConfigureRoute { get; set; }

    public List<Func<IServiceProvider, IOperationInterceptor>> InterceptorFactories { get; } = new();

    public DynamicEntityGroupBuilder EntityGroup { get; set; } = null!;

    public DynamicApiOperation UseInterceptor<TInterceptor>()
        where TInterceptor : IOperationInterceptor
    {
        InterceptorFactories.Add(s => ActivatorUtilities.GetServiceOrCreateInstance<TInterceptor>(s));
        return this;
    }

    public IEnumerable<PropertyMetadata> AllowedProperties { get; set; } = Enumerable.Empty<PropertyMetadata>();
}


