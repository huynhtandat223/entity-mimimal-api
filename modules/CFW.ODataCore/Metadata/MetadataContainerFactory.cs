﻿using CFW.ODataCore.Attributes;
using CFW.ODataCore.Models;
using Microsoft.OData.ModelBuilder;
using System.Reflection;

namespace CFW.ODataCore.Core;

public record EntityEndpointKey
{
    public string RoutePrefix { get; init; } = string.Empty;

    public string Name { get; init; } = string.Empty;
}

public class MetadataContainerFactory : IAssemblyResolver
{
    private static readonly List<Type> _cachedType = AppDomain.CurrentDomain.GetAssemblies()
        .Where(a => !a.IsDynamic && !string.IsNullOrWhiteSpace(a.Location))
        .SelectMany(a => a.GetTypes())
        .Where(x => x.GetCustomAttributes<BaseRoutingAttribute>().Any())
        .ToList();

    public IEnumerable<Type> CacheType { protected set; get; } = _cachedType;

    public IEnumerable<Assembly> Assemblies => _cachedType.Select(x => x.Assembly).Distinct();

    public Dictionary<EntityEndpointKey, List<EntityV2Attribute>> PopulateEntityEndpointAttributes(string sanitizedRoutePrefix)
    {
        var entityEndpoinConfigs = CacheType
            .SelectMany(x => x.GetCustomAttributes<EntityV2Attribute>()
            .Aggregate(new List<EntityV2Attribute>(), (list, attr) =>
            {
                attr.TargetType = x;
                list.Add(attr);
                return list;
            }))
            .GroupBy(x => new EntityEndpointKey
            {
                RoutePrefix = x.RoutePrefix ?? sanitizedRoutePrefix,
                Name = x.Name
            }).ToDictionary(x => x.Key, x => x.ToList());

        return entityEndpoinConfigs;
    }

    public IEnumerable<ODataMetadataContainer> CreateContainers(IServiceCollection services
        , string defaultRoutePrefix, EntityMimimalApiOptions coreOptions)
    {
        var routingAttributes = CacheType
            .SelectMany(x => x.GetCustomAttributes<BaseRoutingAttribute>()
                .Select(attr => new { TargetType = x, RoutingAttribute = attr }))
            .GroupBy(x => x.RoutingAttribute.RoutePrefix ?? defaultRoutePrefix)
            .ToDictionary(x => new ODataMetadataContainer(x.Key), x => x.ToList());

        foreach (var (container, routingInfoInContainer) in routingAttributes)
        {
            routingInfoInContainer
                .Where(x => x.RoutingAttribute is EntityAttribute)
                .Aggregate(container, (currentContainer, x) =>
                {
                    currentContainer.CreateOrEditEntityMetadata(x.TargetType, (EntityAttribute)x.RoutingAttribute);
                    return currentContainer;
                });

            routingInfoInContainer
                .Where(x => x.RoutingAttribute is BoundOperationAttribute)
                .Aggregate(container, (currentContainer, x) =>
                {
                    currentContainer.CreateEntityOpration(x.TargetType, (BoundOperationAttribute)x.RoutingAttribute);
                    return currentContainer;
                });

            routingInfoInContainer
                .Where(x => x.RoutingAttribute is UnboundOperationAttribute)
                .Aggregate(container, (currentContainer, x) =>
                {
                    currentContainer.CreateUnboundOperation(x.TargetType, (UnboundOperationAttribute)x.RoutingAttribute);
                    return currentContainer;
                });

            routingInfoInContainer
               .Where(x => x.RoutingAttribute is ConfigurableEntityAttribute)
               .Aggregate(container, (currentContainer, x) =>
               {
                   currentContainer.CreateDynamicEntityMetadata(x.TargetType, (ConfigurableEntityAttribute)x.RoutingAttribute);
                   return currentContainer;
               });

            container.BuildEdmModel(coreOptions);

            container.RegisterRoutingServices(services);

            yield return container;
        }
    }
}
