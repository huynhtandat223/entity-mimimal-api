using CFW.Core.Utils;
using CFW.EntityApi.Attributes;
using CFW.EntityApi.Queries;
using CFW.EntityApi.Registrators;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using System.Reflection;

namespace CFW.EntityApi.Models.Builders;

public class EntityAttributeApiBuilder : IEntityMemberApiBuilder
{
    private ITypesResolver? _typesResolver;

    public void Build(ContainerRegistrationContext containerRegistrationContext)
    {
        using var scope = containerRegistrationContext.RootServiceProvider.CreateScope();
        var entityAttributes = _typesResolver!.CachedTypes
            .Where(x => x.GetCustomAttributes<EntityAttribute>() is not null)
            .Aggregate(new List<EntityAttribute>(), (acc, x) =>
            {
                var attributes = x.GetCustomAttributes<EntityAttribute>();
                foreach (var attribute in attributes)
                {
                    attribute.TargetType = x;
                    if (attribute.Methods == null || attribute.Methods.Length == 0)
                    {
                        attribute.Methods = [ApiMethod.Query, ApiMethod.Patch, ApiMethod.GetByKey
                            , ApiMethod.Put, ApiMethod.Delete];
                    }
                }

                return acc;
            })
            .ToList();

        if (!entityAttributes.Any())
            return;

        var dbEntityAttributes = entityAttributes
            .Where(x => x is DbEntityAttribute)
            .Cast<DbEntityAttribute>()
            .ToList();

        var dbContextTypes = dbEntityAttributes
            .Select(x => x.DbContextType)
            .Distinct()
            .ToList();

        var dbContextModels = new Dictionary<Type, IModel>();
        if (dbContextTypes.Any())
        {
            foreach (var dbContextType in dbContextTypes)
            {
                var db = (DbContext)scope.ServiceProvider.GetRequiredService(dbContextType);
                dbContextModels.Add(dbContextType, db.Model);
            }
        }

        var attributeGroups = entityAttributes
            .GroupBy(x => x.Name);
        foreach (var attributesByName in attributeGroups)
        {
            if (attributesByName.Key.IsNullOrWhiteSpace())
            {
                throw new InvalidOperationException($"EntityAttribute Name of {attributesByName.First().TargetType} cannot be empty");
            }

            var routeName = StringUtils.SanitizeRoute(attributesByName.Key!);
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

            BuildQueryEntities(attributesByName, dbContextModels, memberRouteContext, scope.ServiceProvider);
        }
    }

    private void BuildQueryEntities(IEnumerable<EntityAttribute> entityAttributes
        , Dictionary<Type, IModel> dbContextModels
        , ContainerMemberRegistrationContext memberRouteContext
        , IServiceProvider serviceProvider)
    {
        var queryableAttributes = entityAttributes
                .Where(x => x.Methods!.Contains(ApiMethod.Query))
                .ToList();

        foreach (var attribute in queryableAttributes)
        {
            if (attribute is DbEntityAttribute dbEntityAttribute)
            {
                var dbContextType = dbEntityAttribute.DbContextType;
                var dbModel = dbContextModels[dbContextType];
                var dbEntityType = dbModel.FindEntityType(dbEntityAttribute.EntityType);

                if (dbEntityType is null)
                {
                    throw new InvalidOperationException($"Entity {dbEntityAttribute.EntityType} not found " +
                        $"in DbContext {dbContextType}");
                }

                var primaryKey = dbEntityType.FindPrimaryKey()!.Properties.Single();
                var dbEntityApiQueryRouterType = typeof(IDbEntityApiQueryRouter<,,>)
                    .MakeGenericType(dbContextType, dbEntityType.ClrType, primaryKey.ClrType);

                var dbMemberRouter = (IDbEntityApiQueryRouter)serviceProvider
                    .GetRequiredService(dbEntityApiQueryRouterType);

                dbMemberRouter.KeyProperty = primaryKey;
                dbMemberRouter.EntityType = dbEntityType;

                memberRouteContext.MemberRouter = dbMemberRouter;
                dbMemberRouter.Register(memberRouteContext);
            }

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

            memberRouteContext.MemberRouter = memberRouter;
            memberRouter.Register(memberRouteContext);
        }
    }
}
