using CFW.ODataCore.Attributes;
using CFW.ODataCore.Intefaces;
using CFW.ODataCore.Models.Metadata;
using Microsoft.OData.ModelBuilder;
using System.Reflection;

namespace CFW.ODataCore.Models;

public class MetadataContainerFactory : IAssemblyResolver
{
    private static readonly List<Type> _cachedType = AppDomain.CurrentDomain.GetAssemblies()
        .Where(a => !a.IsDynamic && !string.IsNullOrWhiteSpace(a.Location))
        .SelectMany(a => a.GetTypes())
        .Where(x => x.GetCustomAttributes<BaseRoutingAttribute>().Any())
        .ToList();

    public IEnumerable<Type> CacheType { protected set; get; } = _cachedType;

    public IEnumerable<Assembly> Assemblies => _cachedType.Select(x => x.Assembly).Distinct();

    private static Type[] _entityHandlerTypes = [typeof(IEntityCreationHandler<>)];

    public MetadataContainer CreateMetadataContainer(EntityMimimalApiOptions mimimalApiOptions)
    {
        var entityEndpoinConfigs = CacheType
            .SelectMany(x => x.GetCustomAttributes<EntityAttribute>()
            .Where(x => x.RoutePrefix == mimimalApiOptions.DefaultRoutePrefix)
            .Aggregate(new List<EntityAttribute>(), (list, attr) =>
            {
                attr.TargetType = x;
                list.Add(attr);
                return list;
            }));

        var handlerAttributes = CacheType
            .SelectMany(x => x.GetCustomAttributes<EntityHandlerAttribute>()
            .Where(x => x.RoutePrefix == mimimalApiOptions.DefaultRoutePrefix)
            .Aggregate(new List<EntityHandlerAttribute>(), (list, attr) =>
            {
                var interfaces = x.GetInterfaces().Where(i => i.IsGenericType
                    && _entityHandlerTypes.Contains(i.GetGenericTypeDefinition()));

                if (!interfaces.Any())
                    throw new InvalidOperationException($"Entity handler {x.FullName} not implement any entity handler interface");

                attr.TargetType = x;
                attr.InterfaceTypes = interfaces.ToArray();
                list.Add(attr);
                return list;
            }));


        var operationHandlerTypes = new[] { typeof(IOperationHandler<>), typeof(IOperationHandler<,>) };
        var boundOperationConfigs = CacheType
            .SelectMany(x => x.GetCustomAttributes<EntityActionAttribute>()
            .Aggregate(new List<MetadataEntityAction>(), (list, attr) =>
            {
                if (x.IsAbstract)
                    return list;

                var interfaces = x.GetInterfaces().Where(i => i.IsGenericType
                    && operationHandlerTypes.Contains(i.GetGenericTypeDefinition()));

                if (!interfaces.Any())
                    throw new InvalidOperationException($"Entity action {attr.ActionName} " +
                        $"handler {x.FullName} not implement any operation interface");

                if (interfaces.Count() > 1)
                    throw new InvalidOperationException($"Entity action {attr.ActionName} " +
                        $"handler {x.FullName} implement multiple operation interface");

                var metadata = new MetadataEntityAction
                {
                    ImplementedInterface = interfaces.First(),
                    RoutePrefix = attr.RoutePrefix,
                    TargetType = x,
                    ActionName = attr.ActionName,
                    HttpMethod = attr.HttpMethod,
                    BoundEntityType = attr.BoundEntityType,
                    EntityName = attr.EntityName
                };

                list.Add(metadata);
                return list;
            }));

        var unboundOperationConfigs = CacheType
            .SelectMany(x => x.GetCustomAttributes<UnboundActionAttribute>()
            .Aggregate(new List<MetadataUnboundAction>(), (list, attr) =>
            {
                if (x.IsAbstract)
                    return list;

                var interfaces = x.GetInterfaces().Where(i => i.IsGenericType
                    && operationHandlerTypes.Contains(i.GetGenericTypeDefinition()));
                if (!interfaces.Any())
                    throw new InvalidOperationException($"Unbound action {attr.ActionName} " +
                        $"handler {x.FullName} not implement any operation interface");

                if (interfaces.Count() > 1)
                    throw new InvalidOperationException($"Unbound action {attr.ActionName} " +
                        $"handler {x.FullName} implement multiple operation interface");

                var metadata = new MetadataUnboundAction
                {
                    ImplementedInterface = interfaces.First(),
                    RoutePrefix = attr.RoutePrefix,
                    TargetType = x,
                    ActionName = attr.ActionName,
                    HttpMethod = attr.ActionMethod
                };
                list.Add(metadata);
                return list;
            }));

        var routePrefix = entityEndpoinConfigs.Select(x => x.RoutePrefix)
            .Concat(handlerAttributes.Select(x => x.RoutePrefix))
            .Concat(boundOperationConfigs.Select(x => x.RoutePrefix))
            .Concat(unboundOperationConfigs.Select(x => x.RoutePrefix))
            .Distinct()
            .Single();

        var metadataContainer = new MetadataContainer(routePrefix, mimimalApiOptions);

        var entityEndpoints = entityEndpoinConfigs.SelectMany(x => x.Methods!.Select(m => new { Attribute = x, Method = m }))
            .GroupBy(x => new { x.Attribute.Name, x.Attribute.TargetType, x.Attribute.AllowedQueryOptions });

        foreach (var key in entityEndpoints)
        {
            var duplicateMethods = key.GroupBy(x => x.Method)
                .Where(x => x.Count() > 1)
                .Select(x => x.Key)
                .ToArray();
            if (duplicateMethods.Any())
                throw new InvalidOperationException($"Duplicate methods {string.Join(",", duplicateMethods)} for entity {key.Key.Name}");

            var dbContextTypes = key.Where(x => x.Attribute.DbContextType is not null)
                .Select(x => x.Attribute.DbContextType)
                .ToList();
            if (dbContextTypes.Count > 1)
                throw new InvalidOperationException($"Multiple DbContextType {string.Join(",", dbContextTypes)} for entity {key.Key.Name}");

            var dbContextType = dbContextTypes.FirstOrDefault() ?? mimimalApiOptions.DbContextOptions?.DbContextType;
            var endpoint = key.Key.Name;
            var allowedQueryOptions = key.Key.AllowedQueryOptions;

            //find entity type
            var sourceType = key.Key.TargetType!;

            if (sourceType.BaseType is not null
                && sourceType.BaseType.IsGenericType
                && sourceType.BaseType.GetGenericTypeDefinition() == typeof(EntityEndpoint<>))
            {
                sourceType = sourceType.BaseType.GetGenericArguments().First();
            }

            var handlerTypes = handlerAttributes
                .Where(x => x.EntityType == sourceType && (x.Name.IsNullOrWhiteSpace() || x.Name == endpoint))
                .ToArray();

            var metadataEntity = new MetadataEntity
            {
                DbContextType = dbContextType,
                HandlerAttributes = handlerTypes,
                Name = endpoint,
                Methods = key.Select(x => x.Method).ToArray(),
                SourceType = sourceType,
                Container = metadataContainer,
                ODataQueryOptions = new ODataQueryOptions { AllowedQueryOptions = allowedQueryOptions }
            };
            metadataContainer.MetadataEntities.Add(metadataEntity);
        }

        foreach (var entityOperationMetadata in boundOperationConfigs)
        {

            var entityName = entityOperationMetadata.EntityName;
            var boundEntityType = entityOperationMetadata.BoundEntityType;

            var entityMetadataList = metadataContainer.MetadataEntities
                .Where(x => x.SourceType == boundEntityType);
            MetadataEntity? boundedEntityMetadata = null;
            if (entityMetadataList.Any())
            {
                if (entityMetadataList.Count() > 1 && entityName.IsNullOrWhiteSpace())
                    throw new InvalidOperationException($"Entity {entityOperationMetadata.BoundEntityType} has " +
                        $"multiple entity attribute apply on it, please specify entity name");

                boundedEntityMetadata = entityMetadataList.SingleOrDefault(x => x.Name == entityName);

                if (boundedEntityMetadata is null)
                    throw new InvalidOperationException($"Entity {entityOperationMetadata.BoundEntityType} has " +
                        $"no entity attribute with name {entityName}");
            }

            if (boundedEntityMetadata is null)
                throw new InvalidOperationException($"Entity {entityOperationMetadata.BoundEntityType} has " +
                    $"no entity attribute apply on it");

            boundedEntityMetadata.Operations.Add(entityOperationMetadata);
        }

        foreach (var containerGroup in unboundOperationConfigs.GroupBy(x => x.RoutePrefix))
        {
            var unboundOperations = containerGroup
                .GroupBy(x => new { x.TargetType, x.ActionName });

            foreach (var unboundOperationMetadata in unboundOperations)
            {
                if (unboundOperationMetadata.Count() > 1)
                    throw new NotImplementedException($"Duplicate unbound operation {unboundOperationMetadata.Key.ActionName}");

                var unboundActionMetadataItem = unboundOperationMetadata.Single();

                metadataContainer.UnboundOperations.Add(unboundActionMetadataItem);
            }
        }

        return metadataContainer;
    }
}
