using CFW.EntityApi.Registrators;
using Humanizer;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

namespace CFW.EntityApi.Models.Builders;

public record DbEntityKey(Type EntityType);

public class EntityFrameworkPopuplationFeature<TDbContext> : IApiFeature
    where TDbContext : DbContext
{
    private readonly ContainerConfiguration _containerConfiguration;

    private Dictionary<DbEntityKey, Action<EntityApiConfiguration>> _customEntityConfigurations
        = new Dictionary<DbEntityKey, Action<EntityApiConfiguration>>();

    public EntityFrameworkPopuplationFeature(ContainerConfiguration containerConfiguration)
    {
        _containerConfiguration = containerConfiguration;
        _containerConfiguration.ApiFeatures.Add(this);
    }

    private Func<IEntityType, string> _routeNameFormater { get; set; }
        = (entityType) =>
        {
            var name = entityType.ClrType.Name;

            if (name.EndsWith("`1") || name.EndsWith("`2") || name.EndsWith("`3"))
                name = name.Substring(0, name.Length - 2);

            return name.Pluralize().Kebaberize();
        };

    internal Func<IEntityType, bool> EntitiesSelector { get; set; } = x => true;

    public int? AutoGenerateEndpointNestedLevel { get; private set; }

    public EntityFrameworkPopuplationFeature<TDbContext> UseRouteNameGenerationStrategy(Func<IEntityType, string> routeNameFormatter)
    {
        _routeNameFormater = routeNameFormatter;
        return this;
    }

    public EntityFrameworkPopuplationFeature<TDbContext> UseNestedLevel(int nestedLevel)
    {
        AutoGenerateEndpointNestedLevel = nestedLevel;
        return this;
    }

    public EntityFrameworkPopuplationFeature<TDbContext> ConfigureApi<TEntity>(
        Action<EntityApiConfiguration<TDbContext, TEntity>> entityApiSetup)
        where TEntity : class
    {
        var key = new DbEntityKey(typeof(TEntity));
        if (_customEntityConfigurations.ContainsKey(key))
            throw new InvalidOperationException($"Entity {typeof(TEntity).Name} already configured");

        _customEntityConfigurations.Add(key, x =>
        {
            var entityApi = x as EntityApiConfiguration<TDbContext, TEntity>;
            entityApiSetup(entityApi!);
        });

        return this;
    }

    public Task Register(ContainerRegistrationContext containerRegistrationContext)
    {
        using var scope = containerRegistrationContext.RootServiceProvider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<TDbContext>();
        var model = db.Model;

        var entityActions = containerRegistrationContext.TypeResolver.EntityActionAttributes;
        var customEntityConfigurations = containerRegistrationContext.ContainerConfiguration.CustomEntityImplementations;

        var entityTypes = db.Model.GetEntityTypes()
            .Where(x => x.FindPrimaryKey() is not null
                && x.FindPrimaryKey()!.Properties.Count == 1) //only support single key entity
            .Where(EntitiesSelector)
            .ToList();

        var configurations = scope.ServiceProvider.GetServices<EntityApiConfiguration>();
        foreach (var apiConfiguration in configurations)
        {
            var routeName = apiConfiguration.RouteName!;

            var router = containerRegistrationContext.ContainerGroupRoute
                    .MapGroup(routeName)
                    .WithMetadata(apiConfiguration!);

            var memberRouteContext = new ContainerMemberRegistrationContext
            {
                ContainerRegistrationContext = containerRegistrationContext,
                MemberRouteGroup = router,
                Name = routeName,
                EntityConfiguration = apiConfiguration!
            };

            apiConfiguration!.RegisterRoutes(memberRouteContext);

            //actions
            var actions = entityActions
                .Where(x => x.EntityName == routeName)
                .ToList();
            memberRouteContext.Actions = actions;
            if (actions.Any())
            {
                var actionRouter = ActivatorUtilities.CreateInstance<Actions.Route>(scope.ServiceProvider)!;
                actionRouter.Register(router, actions);
            }
        }

        foreach (var entityType in entityTypes)
        {
            var primaryKey = entityType.FindPrimaryKey()!.Properties.Single();
            var apiConfigurationType = typeof(EntityApiConfiguration<,,>)
                    .MakeGenericType(typeof(TDbContext), entityType.ClrType, primaryKey.ClrType);

            var apiConfiguration = (EntityApiConfiguration)Activator
                .CreateInstance(apiConfigurationType, entityType, primaryKey)!;

            //Configure from EFCoreFeature Builder.
            var customEntityConfigurationKey = new DbEntityKey(entityType.ClrType);
            if (_customEntityConfigurations.TryGetValue(customEntityConfigurationKey, out var customEntityConfiguration))
            {
                customEntityConfiguration.Invoke(apiConfiguration);
            }

            var routeName = apiConfiguration?.RouteName ?? _routeNameFormater(entityType);

            var router = containerRegistrationContext.ContainerGroupRoute
                    .MapGroup(routeName)
                    .WithMetadata(apiConfiguration!);

            var memberRouteContext = new ContainerMemberRegistrationContext
            {
                ContainerRegistrationContext = containerRegistrationContext,
                MemberRouteGroup = router,
                Name = routeName,
                EntityConfiguration = apiConfiguration!
            };

            apiConfiguration!.RegisterRoutes(memberRouteContext);

            //actions
            var actions = entityActions
                .Where(x => x.EntityName == routeName)
                .ToList();
            memberRouteContext.Actions = actions;
            if (actions.Any())
            {
                var actionRouter = ActivatorUtilities.CreateInstance<Actions.Route>(scope.ServiceProvider)!;
                actionRouter.Register(router, actions);
            }
        }

        return Task.CompletedTask;
    }
}
