using CFW.EntityApi.Attributes;
using CFW.EntityApi.Intefaces;
using CFW.EntityApi.Models.Builders;
using CFW.EntityApi.Registrators;
using System.Reflection;

namespace CFW.EntityApi.Models;

public interface ITypesResolver
{
    public IList<Type> CachedTypes { get; }

    public IEnumerable<EntityAttribute> DbEntityAttributes { get; }

    public IEnumerable<EntityApiConfiguration> ConfiguredEntities { get; }

    public IEnumerable<EntityActionAttribute> EntityActionAttributes { get; }

    public IEnumerable<ActionAttribute> ActionAttributes { get; }
}

public class DefaultTypesResolver : ITypesResolver
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ContainerConfiguration _containerConfiguration;

    private static IEnumerable<Type> operationTypes =
        [
            typeof(IOperationHandler<>),
            typeof(IOperationHandler<,>),
        ];

    public DefaultTypesResolver(IServiceProvider serviceProvider, ContainerConfiguration containerConfiguration)
    {
        _serviceProvider = serviceProvider;
        _containerConfiguration = containerConfiguration;
    }

    public IList<Type> CachedTypes => AppDomain.CurrentDomain
        .GetAssemblies()
        .SelectMany(x => x.GetExportedTypes())
        .Where(x => x.GetCustomAttributes<BaseRoutingAttribute>() is not null)
        .ToList();

    public IEnumerable<EntityAttribute> DbEntityAttributes => GetDbEntityAttributes();

    public IEnumerable<EntityApiConfiguration> ConfiguredEntities => GetConfiguredEntities();

    public IEnumerable<EntityActionAttribute> EntityActionAttributes => GetEntityActionAttributes();

    public IEnumerable<ActionAttribute> ActionAttributes => GetActionAttributes();

    private IEnumerable<ActionAttribute> GetActionAttributes()
    {
        var entityActionAttributes = CachedTypes
            .Where(x => x.GetCustomAttributes<ActionAttribute>() is not null)
            .SelectMany(x => x.GetCustomAttributes<ActionAttribute>()
                .Where(x => x is not EntityActionAttribute)
                .Select(a => new { TargetType = x, Attribute = a }))
            .Aggregate(new List<ActionAttribute>(), (acc, x) =>
            {
                var interfaceType = x.TargetType.GetInterfaces()
                    .SingleOrDefault(i => i == typeof(IOperationHandler) ||
                    (i.IsGenericType && operationTypes.Contains(i.GetGenericTypeDefinition())));

                if (interfaceType is null)
                    throw new InvalidOperationException("Entity action must implement IOperationHandler");

                x.Attribute.TargetType = x.TargetType;
                x.Attribute.InterfaceType = interfaceType;
                acc.Add(x.Attribute);
                return acc;
            });

        return entityActionAttributes;
    }

    private IEnumerable<EntityActionAttribute> GetEntityActionAttributes()
    {
        var operationTypes = new[]
        {
            typeof(IOperationHandler<>),
            typeof(IOperationHandler<,>),
        };

        var entityActionAttributes = CachedTypes
            .Where(x => x.GetCustomAttributes<EntityActionAttribute>() is not null)
            .SelectMany(x => x.GetCustomAttributes<EntityActionAttribute>().Select(a => new { TargetType = x, Attribute = a }))
            .Aggregate(new List<EntityActionAttribute>(), (acc, x) =>
            {
                var interfaceType = x.TargetType.GetInterfaces()
                    .SingleOrDefault(i => i == typeof(IOperationHandler) ||
                    (i.IsGenericType && operationTypes.Contains(i.GetGenericTypeDefinition())));

                if (interfaceType is null)
                    throw new InvalidOperationException("Entity action must implement IOperationHandler");

                x.Attribute.TargetType = x.TargetType;
                x.Attribute.InterfaceType = interfaceType;
                acc.Add(x.Attribute);
                return acc;
            });

        return entityActionAttributes;
    }

    private IEnumerable<EntityAttribute> GetDbEntityAttributes()
    {
        if (_containerConfiguration.AttributeApiBuilder is null)
            return new List<EntityAttribute>();

        var dbEntityAttributes = CachedTypes
            .Where(x => x.GetCustomAttributes<EntityAttribute>() is not null)
            .SelectMany(x => x.GetCustomAttributes<EntityAttribute>().Select(a => new { TargetType = x, Attribute = a }))
            .Aggregate(new List<EntityAttribute>(), (acc, x) =>
            {
                x.Attribute.TargetType = x.TargetType;
                acc.Add(x.Attribute);
                return acc;
            });

        return dbEntityAttributes;
    }

    private IEnumerable<EntityApiConfiguration> GetConfiguredEntities()
    {
        var entityApiBuilders = CachedTypes
            .Where(x => x.GetCustomAttributes<EntityConfigurationAttribute>() is not null)
            .SelectMany(x => x.GetCustomAttributes<EntityConfigurationAttribute>()
                .Select(a => new { TargetType = x, Attribute = a }))
            .Aggregate(new List<EntityApiConfiguration>(), (acc, x) =>
            {
                var interfaceType = x.TargetType.GetInterfaces()
                    .SingleOrDefault(i => i == typeof(IEntityApiConfiguration));

                if (interfaceType is null)
                    throw new InvalidOperationException("Entity configuration must implement IEntityApiConfiguration");

                var instance = (IEntityApiConfiguration)ActivatorUtilities
                    .CreateInstance(_serviceProvider, x.TargetType);
                var entityBuilder = new EntityApiContextBuilder();
                instance.Configure(entityBuilder);

                acc.AddRange(entityBuilder.EntityApiConfigurations);
                return acc;
            });

        return entityApiBuilders;
    }
}
