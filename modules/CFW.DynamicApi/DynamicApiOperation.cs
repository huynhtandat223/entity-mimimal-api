using CFW.Core.Utils;
using CFW.DynamicApi.Buiders;
using CFW.DynamicApi.Interceptors;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using System.Linq.Expressions;

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
    internal readonly List<string> _excludeProperties = new List<string>();
    internal readonly List<string> _includeProperties = new List<string>();

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

    public DynamicApiOperation IncludeProperties<TEntity>(params Expression<Func<TEntity, object>>[] properties)
    {
        foreach (var prop in properties)
        {
            var name = ExpressionUtils.GetPropertyName(prop);
            _includeProperties.Add(name);
        }
        return this;
    }

    public DynamicApiOperation ExcludeProperties<TEntity>(params Expression<Func<TEntity, object>>[] properties)
    {
        foreach (var prop in properties)
        {
            var name = ExpressionUtils.GetPropertyName(prop);
            _excludeProperties.Add(name);
        }
        return this;
    }

    public virtual void MapApi(RouteGroupBuilder group)
    {
        var operation = this;
        var route = group.MapMethods(operation.Route, [operation.HttpMethod], async (HttpContext ctx) =>
        {
            // Initialize interceptors once
            var interceptors = operation.InterceptorFactories
                .Select(factory => factory(ctx.RequestServices))
                .ToList();

            // Execute OnExecutingAsync for all interceptors
            foreach (var interceptor in interceptors)
            {
                await interceptor.OnExecutingAsync(ctx, operation);
            }

            // Execute the handler
            var result = await operation.Handler!(ctx);

            // Execute OnExecutedAsync for all interceptors
            var interceptedResult = default(object?);
            foreach (var interceptor in interceptors)
            {
                interceptedResult = await interceptor.OnExecutedAsync(ctx, operation, result);
            }

            return interceptedResult;

        }).WithMetadata(operation);
    }

    public IEnumerable<PropertyMetadata> AllowedProperties { get; set; } = Enumerable.Empty<PropertyMetadata>();
}

public class DynamicApiOperation<TKey> : DynamicApiOperation
{
    public Func<HttpContext, TKey, Task<object?>>? ModelHandler { get; set; }

    public override void MapApi(RouteGroupBuilder group)
    {
        var operation = this;
        var route = group.MapMethods(operation.Route, [operation.HttpMethod], async (HttpContext ctx, TKey key) =>
        {
            // Initialize interceptors once
            var interceptors = operation.InterceptorFactories
                .Select(factory => factory(ctx.RequestServices))
                .ToList();

            // Execute OnExecutingAsync for all interceptors
            foreach (var interceptor in interceptors)
            {
                await interceptor.OnExecutingAsync(ctx, operation);
            }

            // Execute the handler
            var result = await operation.ModelHandler!(ctx, key);

            // Execute OnExecutedAsync for all interceptors
            object? interceptedResult = null;
            foreach (var interceptor in interceptors)
            {
                interceptedResult = await interceptor.OnExecutedAsync(ctx, operation, result);
            }

            return interceptedResult;
        }).WithMetadata(operation);
    }
}