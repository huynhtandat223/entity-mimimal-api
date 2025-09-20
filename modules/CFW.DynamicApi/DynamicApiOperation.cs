using CFW.Core.Utils;
using CFW.DynamicApi.Buiders;
using CFW.DynamicApi.Interceptors;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using System.Linq.Expressions;

namespace CFW.DynamicApi;

public class DynamicApiOperation
{
    internal readonly List<string> _excludeProperties = new List<string>();
    internal readonly List<string> _includeProperties = new List<string>();

    public string HttpMethod { get; set; } = "GET";

    public string Route { get; set; } = "/";

    internal Type? TargetType { get; set; }

    public string? Name { get; set; }

    [Obsolete("Not lexible enough")]
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

    public virtual RouteHandlerBuilder MapApi(RouteGroupBuilder group)
    {
        var operation = this;
        return group.MapMethods(operation.Route, [operation.HttpMethod], async (HttpContext ctx) =>
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
            object? interceptedResult = result;
            foreach (var interceptor in interceptors)
            {
                interceptedResult = await interceptor.OnExecutedAsync(ctx, operation, result);
            }

            return interceptedResult;

        }).WithMetadata(operation);
    }

    public IEnumerable<PropertyMetadata> AllowedProperties { get; set; } = Enumerable.Empty<PropertyMetadata>();

    public bool AllowAllProperties { set; get; } = false;
    public Type? RequestType { get; set; }
    public Type? ResponseType { get; set; }
}

public class DynamicApiOperation<TKey> : DynamicApiOperation
{
    public Func<HttpContext, TKey, Task<object?>>? ModelHandler { get; set; }

    public override RouteHandlerBuilder MapApi(RouteGroupBuilder group)
    {
        var operation = this;
        return group.MapMethods(operation.Route, [operation.HttpMethod], async (HttpContext ctx, TKey key) =>
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
            object? interceptedResult = result;
            foreach (var interceptor in interceptors)
            {
                interceptedResult = await interceptor.OnExecutedAsync(ctx, operation, result);
            }

            return interceptedResult;
        }).WithMetadata(operation);
    }
}

public class ApiOperation<TRequest, TResponse> : DynamicApiOperation
{
    public ApiOperation(Type targetType)
    {
        TargetType = targetType;
    }

    public override RouteHandlerBuilder MapApi(RouteGroupBuilder group)
    {
        var operation = this;

#pragma warning disable CS8714 // The type cannot be used as type parameter in the generic type or method. Nullability of type argument doesn't match 'notnull' constraint.
        return group.MapMethods(operation.Route ?? "/", [operation.HttpMethod]
            , async (HttpContext ctx, QueryRequest<TRequest> request) =>
        {
            if (request is null)
                return Results.BadRequest("Invalid Request");

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
            var handler = ActivatorUtilities.CreateInstance(ctx.RequestServices, TargetType)
            as IRequestHandler<TRequest, TResponse>;
            var requestContext = new RequestModel<TRequest>
            {
                Model = request.QueryModel!,
                ServiceProvider = ctx.RequestServices
            };
            var result = await handler!.Handle(requestContext, ctx.RequestAborted);
            if (!result.IsSuccess)
                return TypedResults.BadRequest(result.Message);

            // Execute OnExecutedAsync for all interceptors
            object? interceptedResult = result.Data;
            foreach (var interceptor in interceptors)
            {
                interceptedResult = await interceptor.OnExecutedAsync(ctx, operation, result);
            }

            return interceptedResult;

        }).Produces<TResponse>(StatusCodes.Status200OK)
        .Accepts<TRequest>("application/json")
        .Produces(StatusCodes.Status400BadRequest)
        .WithMetadata(operation);
#pragma warning restore CS8714 // The type cannot be used as type parameter in the generic type or method. Nullability of type argument doesn't match 'notnull' constraint.
    }
}