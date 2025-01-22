﻿using CFW.ODataCore.Intefaces;
using CFW.ODataCore.RouteMappers.Actions;

namespace CFW.ODataCore.Models.Metadata;

public class MetadataUnboundAction : MetadataAction
{

    internal void AddServices(RouteGroupBuilder containerGroupBuilder)
    {
        ResolveRequestResponseTypes();

        //register operation services
        var interfaceType = !HasKey
            ? typeof(IOperationHandler<>).MakeGenericType(RequestType!)
            : typeof(IOperationHandler<,>)
                .MakeGenericType(RequestType!, ResponseType!);
        var implementationType = TargetType;

        ImplementationType = implementationType;
        InterfaceType = interfaceType;

        //register operation routes
        Type? routeMapperType = null;
        if (HasKey && HasResponseData)
            routeMapperType = typeof(DefaultUnboundActionHasResponseRouteMapper<,,>)
                .MakeGenericType(RequestType!, KeyProperty!.PropertyType, ResponseType!);

        if (!HasKey && HasResponseData)
            routeMapperType = typeof(DefaultUnboundActionHasResponseRouteMapper<,>)
                .MakeGenericType(RequestType!, ResponseType!);

        if (HasKey && !HasResponseData)
            routeMapperType = typeof(DefaultUnboundActionRouteMapper<,>)
                .MakeGenericType(RequestType!, KeyProperty!.PropertyType);

        if (!HasKey && !HasResponseData)
            routeMapperType = typeof(DefaultUnboundActionRouteMapper<>)
                .MakeGenericType(RequestType!);

        if (routeMapperType is null)
            throw new InvalidOperationException("Invalid route mapper type");

        var routeMapper = (IRouteMapper)Activator.CreateInstance(routeMapperType, this)!;
        routeMapper.MapRoutes(containerGroupBuilder);
    }
}
