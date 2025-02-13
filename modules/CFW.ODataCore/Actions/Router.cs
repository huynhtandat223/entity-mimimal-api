using CFW.EntityApi.Attributes;
using CFW.EntityApi.Intefaces;
using CFW.EntityApi.Models;
using Microsoft.AspNetCore.Mvc;

namespace CFW.EntityApi.Actions;

public class Route
{
    private readonly IServiceProvider _serviceProvider;
    public Route(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public void Register(RouteGroupBuilder groupBuilder, IEnumerable<ActionAttribute> actions)
    {
        if (!actions.Any())
            return;

        foreach (var actionMetadata in actions)
        {
            var intefaceType = actionMetadata.InterfaceType;
            var operationType = intefaceType!.GetGenericTypeDefinition();

            if (operationType == typeof(IOperationHandler<,>))
            {
                var requestType = intefaceType.GetGenericArguments()[0];
                var responseType = intefaceType.GetGenericArguments()[1];

                var method = typeof(ActionRouteMapperExtensions)
                    .GetMethod(nameof(ActionRouteMapperExtensions.MappHasResponseDataRoutes))!
                    .MakeGenericMethod(requestType, responseType);

                var args = new object[] { groupBuilder, actionMetadata };

                method.Invoke(null, args);
            }
        }
    }


}

public static class ActionRouteMapperExtensions
{
    public static string[] MapHttpMethods(ApiMethod apiMethod)
    {
        return apiMethod switch
        {
            ApiMethod.Get => ["GET"],
            ApiMethod.Post => ["POST"],
            ApiMethod.Put => ["PUT"],
            ApiMethod.Patch => ["PATCH"],
            ApiMethod.Delete => ["DELETE"],
            _ => throw new InvalidOperationException("Invalid Method")
        };
    }

    private static Dictionary<Type, Delegate> _setters = new();

    //public static Task MappRoutes<TRequest>(ContainerMemberRegistrationContext containerMemberRegistrationContext
    //    , RouteGroupBuilder routeGroupBuilder, EntityActionAttribute actionMetadata)
    //{
    //    var mappedMethods = MapHttpMethods(actionMetadata.HttpMethod);
    //    var actionName = actionMetadata.ActionName;

    //    routeGroupBuilder.MapMethods(actionName, mappedMethods, async ([FromBody] TRequest? request
    //        , QueryRequest<TRequest> queryRequest
    //        , HttpContext httpContext, CancellationToken cancellationToken) =>
    //    {
    //        request ??= queryRequest.QueryModel;
    //        if (request is null)
    //            return Results.BadRequest("Invalid Request");

    //        if (request is IRequestMetadata metadata)
    //        {
    //            metadata.Metadata = actionMetadata;
    //        }

    //        var handler = (IOperationHandler<TRequest>)ActivatorUtilities
    //        .CreateInstance(httpContext.RequestServices, actionMetadata.ImplementationType!);

    //        var result = await handler.Handle(request, cancellationToken);
    //        return result.ToResults();
    //    });

    //    return Task.CompletedTask;
    //}

    //public static Task MappRoutes<TRequest, TKey>(ContainerMemberRegistrationContext containerMemberRegistrationContext, RouteGroupBuilder routeGroupBuilder, EntityActionAttribute actionMetadata)
    //{
    //    var mappedMethods = MapHttpMethods(actionMetadata.HttpMethod);
    //    var keyProperty = actionMetadata.KeyProperty!;
    //    var actionName = actionMetadata.ActionName;

    //    var routePattern = actionMetadata is MetadataEntityAction
    //            ? $"{{key}}/{actionName}"
    //            : $"{actionName}/{{key}}";

    //    routeGroupBuilder.MapMethods(routePattern, mappedMethods, async (
    //        [FromBody] TRequest? request, QueryRequest<TRequest> queryRequest
    //        , TKey key, HttpContext httpContext, CancellationToken cancellationToken) =>
    //    {
    //        request ??= queryRequest.QueryModel;
    //        if (request is null)
    //            return Results.BadRequest("Invalid Request");

    //        if (request is IRequestMetadata metadata)
    //        {
    //            metadata.Metadata = actionMetadata;
    //        }

    //        //set key to request
    //        if (!_setters.TryGetValue(keyProperty.PropertyType, out var setter))
    //        {
    //            var expr = keyProperty!.BuildSetter();
    //            setter = expr.Compile();
    //            _setters[keyProperty.PropertyType] = setter;
    //        }
    //        setter.DynamicInvoke(request, key);

    //        var handler = (IOperationHandler<TRequest>)ActivatorUtilities
    //        .CreateInstance(httpContext.RequestServices, actionMetadata.ImplementationType!);

    //        var result = await handler.Handle(request, cancellationToken);
    //        return result.ToResults();
    //    });

    //    return Task.CompletedTask;
    //}

    public static Task MappHasResponseDataRoutes<TRequest, TResponse>(RouteGroupBuilder routeGroupBuilder
        , ActionAttribute actionMetadata)
    {
        var mappedMethods = MapHttpMethods(actionMetadata.Method);
        var actionName = actionMetadata.ActionName;

        routeGroupBuilder.MapMethods(actionName, mappedMethods, async ([FromBody] TRequest? request
            , QueryRequest<TRequest> queryRequest
            , HttpContext httpContext
            , CancellationToken cancellationToken) =>
        {
            request ??= queryRequest.QueryModel;
            if (request is null)
                return Results.BadRequest("Invalid Request");

            //if (request is IRequestMetadata metadata)
            //{
            //    metadata.Metadata = actionMetadata;
            //}

            var handler = (IOperationHandler<TRequest, TResponse>)ActivatorUtilities
                .CreateInstance(httpContext.RequestServices, actionMetadata.TargetType!);

            var result = await handler.Handle(request, cancellationToken);
            return result.ToResults();
        });

        return Task.CompletedTask;
    }

    //public static Task MappHasResponseDataRoutes<TRequest, TKey, TResponse>(RouteGroupBuilder routeGroupBuilder
    //    , MetadataAction actionMetadata)
    //{
    //    var mappedMethods = MapHttpMethods(actionMetadata.HttpMethod);
    //    var keyProperty = actionMetadata.KeyProperty!;
    //    var actionName = actionMetadata.ActionName;

    //    var routePattern = actionMetadata is MetadataEntityAction
    //            ? $"{{key}}/{actionName}"
    //            : $"{actionName}/{{key}}";

    //    routeGroupBuilder.MapMethods(routePattern, mappedMethods, async ([FromBody] TRequest? request
    //        , QueryRequest<TRequest> queryRequest
    //        , TKey key,
    //    HttpContext httpContext, CancellationToken cancellationToken) =>
    //    {
    //        request ??= queryRequest.QueryModel;
    //        if (request is null)
    //            return Results.BadRequest("Invalid Request");

    //        if (request is IRequestMetadata metadata)
    //        {
    //            metadata.Metadata = actionMetadata;
    //        }

    //        //set key to request
    //        if (!_setters.TryGetValue(keyProperty.PropertyType, out var setter))
    //        {
    //            var expr = keyProperty!.BuildSetter();
    //            setter = expr.Compile();
    //            _setters[keyProperty.PropertyType] = setter;
    //        }

    //        setter.DynamicInvoke(request, key);

    //        var handler = (IOperationHandler<TRequest, TResponse>)ActivatorUtilities
    //            .CreateInstance(httpContext.RequestServices, actionMetadata.ImplementationType!);
    //        var result = await handler.Handle(request, cancellationToken);
    //        return result.ToResults();
    //    });

    //    return Task.CompletedTask;
    //}
}
