using CFW.Core.Utils;
using CFW.EntityApi.Attributes;
using CFW.EntityApi.Queries;
using CFW.EntityApi.Registrators;
using Humanizer;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

namespace CFW.EntityApi.Models.Builders;

/// <summary>
/// Build Apis base on Attributes
/// </summary>
[Obsolete("Is this needed?")]
public class AttributeApiBuilder
{
    internal Func<EntityAttribute, string> _routeNameFormater { get; set; }
        = (entityAttribute) =>
        {
            var name = entityAttribute.TargetType.Name;
            return name.Pluralize().Kebaberize();
        };

    internal Type? DefaultDbContextType { get; set; } = typeof(DefaultDbContext);

    public AttributeApiBuilder UseDefaultDbContext<TDbContext>()
        where TDbContext : DbContext
    {
        DefaultDbContextType = typeof(TDbContext);
        return this;
    }

    public Task Build(ContainerRegistrationContext containerRegistrationContext)
    {
        using var scope = containerRegistrationContext.RootServiceProvider.CreateScope();
        var typesResolver = containerRegistrationContext.TypeResolver;

        var dbEntityAttributes = typesResolver.DbEntityAttributes;

        var dbContextTypes = dbEntityAttributes
            .Where(x => x.DbContextType is not null)
            .Select(x => x.DbContextType)
            .Distinct()
            .ToList();

        var dbContextModels = new Dictionary<Type, IModel>();
        if (dbContextTypes.Any())
        {
            foreach (var dbContextType in dbContextTypes)
            {
                var db = (DbContext)scope.ServiceProvider.GetRequiredService(dbContextType!);
                dbContextModels.Add(dbContextType!, db.Model);
            }
        }

        var attributeGroups = dbEntityAttributes
            .GroupBy(x => x.Name);
        foreach (var attributesByName in attributeGroups)
        {
            var dbEntityAtrribute = attributesByName.Single();

            var routeName = attributesByName.Key.IsNullOrWhiteSpace()
                ? _routeNameFormater(dbEntityAtrribute)
                : StringUtils.SanitizeRoute(attributesByName.Key!);

            var router = containerRegistrationContext.ContainerGroupRoute
                    .MapGroup(routeName)
                    .WithTags(routeName)
                    .WithName(routeName);

            var memberRouteContext = new ContainerMemberRegistrationContext
            {
                ContainerRegistrationContext = containerRegistrationContext,
                MemberRouteGroup = router,
                Name = routeName
            };

            BuildQueryEntities(dbEntityAtrribute, dbContextModels, memberRouteContext, scope.ServiceProvider);
        }

        return Task.CompletedTask;
    }

    private void BuildQueryEntities(EntityAttribute attribute
        , Dictionary<Type, IModel> dbContextModels
        , ContainerMemberRegistrationContext memberRouteContext
        , IServiceProvider serviceProvider)
    {
        if (attribute is not EntityAttribute dbEntityAttribute || dbEntityAttribute.DbContextType is null)
        {
            return;
        }

        var dbContextType = dbEntityAttribute.DbContextType;

        var dbModel = dbContextModels[dbContextType];
        var dbEntityType = dbModel.FindEntityType(dbEntityAttribute.TargetType);

        if (dbEntityType is null)
        {
            throw new InvalidOperationException($"Entity {dbEntityAttribute.TargetType} not found " +
                $"in DbContext {dbContextType}");
        }

        var primaryKey = dbEntityType.FindPrimaryKey()!.Properties.Single();
        var dbEntityApiQueryRouterType = typeof(IDbEntityApiQueryRouter<,,>)
            .MakeGenericType(dbContextType, dbEntityType.ClrType, primaryKey.ClrType);

        var dbMemberRouter = (IDbEntityApiQueryRouter)serviceProvider
            .GetRequiredService(dbEntityApiQueryRouterType);

        dbMemberRouter.KeyProperty = primaryKey;
        dbMemberRouter.EntityType = dbEntityType;

        memberRouteContext.QueryMemberRouter = dbMemberRouter;
        dbMemberRouter.Register(memberRouteContext);

        var interfaces = attribute.TargetType.GetInterfaces();
        var entityApiQueryInteface = interfaces.SingleOrDefault(x => x.IsGenericType
            && x.GetGenericTypeDefinition() == typeof(IEntityApiQuery<>));

        if (entityApiQueryInteface is null)
        {
            throw new NotImplementedException();
        }

        var entityType = entityApiQueryInteface.GetGenericArguments().First();
        var entityApiQueryRouterType = typeof(IEntityApiQueryRouter<>)
            .MakeGenericType(entityType);

        var memberRouter = (IContainerMemberRouter)serviceProvider
            .GetRequiredService(entityApiQueryRouterType);

        memberRouteContext.QueryMemberRouter = memberRouter;
        memberRouter.Register(memberRouteContext);
    }
}
