using CFW.ODataCore.Intefaces;
using CFW.ODataCore.RouteMappers.Actions;

namespace CFW.ODataCore.Models.Metadata;

public class MetadataEntityAction : MetadataAction
{
    public required Type BoundEntityType { get; init; }

    public required string? EntityName { get; init; }

    internal void AddServices(RouteGroupBuilder containerRouteGroupBuilder)
    {
        ResolveRequestResponseTypes();

        //register operation services
        var interfaceType = HasResponseData
            ? typeof(IOperationHandler<,>)
                .MakeGenericType(RequestType!, ResponseType!) :
                typeof(IOperationHandler<>).MakeGenericType(RequestType!);
        var implementationType = TargetType;

        InterfaceType = interfaceType;
        ImplementationType = implementationType;

        //register operation routes
        Type? routeMapperType = null;
        if (HasKey && HasResponseData)
            routeMapperType = typeof(DefaultEntityActionHasResponseRequestHandler<,,>)
                .MakeGenericType(RequestType!, KeyProperty!.PropertyType, ResponseType!);

        if (!HasKey && HasResponseData)
            routeMapperType = typeof(DefaultEntityActionHasResponseRequestHandler<,>)
                .MakeGenericType(RequestType!, ResponseType!);

        if (HasKey && !HasResponseData)
            routeMapperType = typeof(DefaultEntityActionRequestHandler<,>)
                .MakeGenericType(RequestType!, KeyProperty!.PropertyType);

        if (!HasKey && !HasResponseData)
            routeMapperType = typeof(DefaultEntityActionRequestHandler<>)
                .MakeGenericType(RequestType!);

        if (routeMapperType is null)
            throw new InvalidOperationException("Invalid route mapper type");

        var routeMapper = (IRouteMapper)Activator.CreateInstance(routeMapperType, this)!;
        routeMapper.MapRoutes(containerRouteGroupBuilder);
    }
}
