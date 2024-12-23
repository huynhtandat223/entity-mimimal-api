﻿using CFW.ODataCore.Features.BoundOperations;
using CFW.ODataCore.Features.EntityCreate;
using CFW.ODataCore.Features.EntityQuery;
using CFW.ODataCore.Features.EntitySets;
using CFW.ODataCore.Features.Shared;
using CFW.ODataCore.Features.UnBoundOperations;
using System.Reflection;

namespace CFW.ODataCore.Core.MetadataResolvers;

public abstract class BaseODataMetadataResolver
{
    protected abstract IEnumerable<Type> CachedType { get; }

    private readonly string _defaultPrefix;

    public BaseODataMetadataResolver(string defaultPrefix)
    {
        _defaultPrefix = defaultPrefix;
    }

    internal IEnumerable<ODataBoundOperationMetadata> GetBoundOperationMetadataList(Type viewModelType, Type keyType
        , ODataMetadataContainer container
        , string boundCollectionName)
    {
        var boundOperationAttr = typeof(BoundOperationAttribute);
        var withResponseHandlerInterfaceType = typeof(IODataOperationHandler<,>);
        var nonResponseInterfaceType = typeof(IODataOperationHandler<>);

        var handlerTypeInfos = CachedType
            .Where(x => x.GetCustomAttribute(boundOperationAttr) is not null)
            .Select(x => new { HandlerType = x, Attribute = x.GetCustomAttribute<BoundOperationAttribute>()! })
            .Where(x => x.Attribute.KeyType == keyType && x.Attribute.ViewModelType == viewModelType)
            .ToArray();

        if (handlerTypeInfos.Length == 0)
            yield break;

        //Check viewModelType routing name is same as boundCollectionName
        var hasDiffEntitySetRouting = handlerTypeInfos
            .Any(x => x.Attribute.ViewModelType.GetCustomAttribute<ODataEntitySetAttribute>() is null
                || x.Attribute.ViewModelType.GetCustomAttribute<ODataEntitySetAttribute>()!.Name != boundCollectionName);
        if (hasDiffEntitySetRouting)
            throw new InvalidOperationException($"Bound operation handler for {viewModelType} has different routing name than the entity set name");

        foreach (var handlerTypeInfo in handlerTypeInfos)
        {
            var handlerType = handlerTypeInfo.HandlerType;
            var routingAttribute = handlerTypeInfo.Attribute;
            var interfaces = handlerType.GetInterfaces();
            var withResponseHandlerInterface = interfaces
                .SingleOrDefault(x => x.IsGenericType && x.GetGenericTypeDefinition() == withResponseHandlerInterfaceType);

            if (withResponseHandlerInterface is not null)
            {
                var requestType = withResponseHandlerInterface.GetGenericArguments().First();
                var responseType = withResponseHandlerInterface.GetGenericArguments().Last();

                yield return new ODataBoundOperationMetadata
                {
                    OperationType = routingAttribute.OperationType,
                    KeyType = keyType,
                    SetupAttributes = handlerType.GetCustomAttributes(),
                    BoundCollectionName = boundCollectionName,
                    Container = container,
                    RequestType = requestType,
                    ResponseType = responseType,
                    HandlerType = handlerType,
                    BoundOprationAttribute = routingAttribute,
                    ControllerType = typeof(BoundOperationsController<,,,>).MakeGenericType(
                        viewModelType, keyType, requestType, responseType).GetTypeInfo(),
                };
            }

            var nonResponseHandlerInterface = interfaces
                .SingleOrDefault(x => x.IsGenericType && x.GetGenericTypeDefinition() == nonResponseInterfaceType);
            if (nonResponseHandlerInterface is not null)
            {
                var requestType = nonResponseHandlerInterface.GetGenericArguments().Single();
                var responseType = typeof(Result);

                yield return new ODataBoundOperationMetadata
                {
                    OperationType = routingAttribute.OperationType,
                    KeyType = keyType,
                    SetupAttributes = handlerType.GetCustomAttributes(),
                    BoundCollectionName = boundCollectionName,
                    Container = container,
                    RequestType = requestType,
                    ResponseType = responseType,
                    HandlerType = handlerType,
                    BoundOprationAttribute = handlerType.GetCustomAttribute<BoundOperationAttribute>()!,
                    ControllerType = typeof(BoundOperationsController<,,,>).MakeGenericType(
                        viewModelType, keyType, requestType, responseType).GetTypeInfo(),
                };
            }
        }
    }

    /// <summary>
    /// Create metadata entity for the given type
    /// </summary>
    /// <param name="applyType">The type apply ODataAPIRoutingAttribute, can be VIewModel or Handler</param>
    /// <param name="routingAttribute"></param>
    /// <param name="container"></param>
    /// <returns></returns>
    private APIMetadata CreateAPIMetadata(Type applyType, ODataAPIRoutingAttribute routingAttribute
        , ODataMetadataContainer container)
    {
        if (routingAttribute is BoundEntityRoutingAttribute boundEntityRoutingAttribute)
        {
            var viewModelType = boundEntityRoutingAttribute.ViewModelType;
            var keyType = boundEntityRoutingAttribute.KeyType;

            if (viewModelType is null)
            {
                var odataViewModelInterface = applyType.GetInterfaces()
                    .Single(x => x.IsGenericType && x.GetGenericTypeDefinition() == typeof(IODataViewModel<>));

                viewModelType = applyType;
                keyType = odataViewModelInterface.GetGenericArguments().Single();
            }

            var controlerInfoByMethod = new Dictionary<ODataMethod, (string ActionName, Type ControllerType)>
            {
                [ODataMethod.PostCreate] = (nameof(EntityCreateController<RefODataViewModel, int>.Post)
                    , typeof(EntityCreateController<,>).MakeGenericType(viewModelType, keyType)),

                [ODataMethod.Query] = (nameof(EntityQueryController<RefODataViewModel, int>.Query)
                    , typeof(EntityQueryController<,>).MakeGenericType(viewModelType, keyType)),

                [ODataMethod.GetByKey] = (nameof(EntityGetByKeyController<RefODataViewModel, int>.GetByKey)
                    , typeof(EntityGetByKeyController<,>).MakeGenericType(viewModelType, keyType)),

                [ODataMethod.PatchUpdate] = (nameof(EntityPatchController<RefODataViewModel, int>.Patch)
                    , typeof(EntityPatchController<,>).MakeGenericType(viewModelType, keyType)),

                [ODataMethod.Delete] = (nameof(EntityDeleteController<RefODataViewModel, int>.Delete)
                    , typeof(EntityDeleteController<,>).MakeGenericType(viewModelType, keyType)),
            };

            return new BoundAPIMetadata
            {
                Method = routingAttribute.Method,
                DbSetType = boundEntityRoutingAttribute.DbSetType,
                RoutingAttribute = routingAttribute,
                Container = container,
                ViewModelType = viewModelType,
                KeyType = keyType,
                ControllerActionMethodName = controlerInfoByMethod[routingAttribute.Method].ActionName,
                ControllerType = controlerInfoByMethod[routingAttribute.Method].ControllerType.GetTypeInfo(),
                BoundOperationMetadataList = GetBoundOperationMetadataList(viewModelType, keyType, container, routingAttribute.Name),
                SetupAttributes = applyType.GetCustomAttributes(),
            };
        }

        throw new NotImplementedException();
    }

    private UnboundOperationMetadata CreateUnboundOperationMetadata(Type handlerType
        , UnboundOperationAttribute unboundOperationAttribute)
    {
        var handlerInterface = handlerType
            .GetInterfaces()
            .SingleOrDefault(x => x.IsGenericType
                && x.GetGenericTypeDefinition() == typeof(IODataOperationHandler<>));

        if (handlerInterface is null)
        {
            handlerInterface = handlerType
                .GetInterfaces()
                .SingleOrDefault(x => x.IsGenericType
                    && x.GetGenericTypeDefinition() == typeof(IODataOperationHandler<,>));
        }

        if (handlerInterface is null)
            throw new InvalidOperationException($"Handler type {handlerType} does not implement IODataActionHandler<> or IODataActionHandler<,>");

        var args = handlerInterface.GetGenericArguments();
        var requestType = args[0];
        var responseType = args.Count() == 1 ? typeof(Result) : args[1];

        return new UnboundOperationMetadata
        {
            HandlerType = handlerType,
            RequestType = requestType,
            ResponseType = responseType,
            Attribute = unboundOperationAttribute,
            SetupAttributes = handlerType.GetCustomAttributes(),
            ControllerType = typeof(UnboundOperationsController<,>).MakeGenericType(requestType, responseType).GetTypeInfo(),
        };
    }

    public IEnumerable<ODataMetadataContainer> CreateContainers()
    {
        var containers = new List<ODataMetadataContainer>();
        var routePrefixes = CachedType
            .SelectMany(x => x.GetCustomAttributes<ODataAPIRoutingAttribute>().Select(c => new { ApplyType = x, RoutingAttribute = c }))
            .GroupBy(x => x.RoutingAttribute!.RouteRefix ?? _defaultPrefix);

        foreach (var group in routePrefixes)
        {
            var routePrefix = group.Key;
            var container = new ODataMetadataContainer(routePrefix);

            var apiMetadata = group
                .Select(x => CreateAPIMetadata(x.ApplyType, x.RoutingAttribute, container));

            container.AddEntitySets(routePrefix, this, apiMetadata);
            containers.Add(container);
        }

        //var unboudOperationMetadataGroup = CachedType
        //    .Where(x => x.GetCustomAttribute<UnboundOperationAttribute>() is not null)
        //    .Select(x => new
        //    {
        //        HandlerType = x,
        //        Attribute = x.GetCustomAttribute<UnboundOperationAttribute>()!,
        //    })
        //    .GroupBy(x => x.Attribute!.RoutePrefix ?? _defaultPrefix);
        //foreach (var group in unboudOperationMetadataGroup)
        //{
        //    var routePrefix = group.Key;
        //    var container = containers.FirstOrDefault(x => x.RoutePrefix == routePrefix);
        //    if (container is null)
        //    {
        //        container = new ODataMetadataContainer(routePrefix);
        //        containers.Add(container);
        //    }

        //    var unboundOperationMetadataList = group
        //        .Select(x => CreateUnboundOperationMetadata(x.HandlerType, x.Attribute))
        //        .ToList();

        //    container.AddUnboundOperations(unboundOperationMetadataList);
        //}

        containers.ForEach(x => x.Build());
        return containers;
    }
}
