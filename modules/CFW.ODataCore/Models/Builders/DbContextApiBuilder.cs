using CFW.EntityApi.Queries;
using CFW.EntityApi.Registrators;
using Humanizer;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

namespace CFW.EntityApi.Models.Builders;

public class DbContextApiBuilder<TDbContext> : IEntityMemberApiBuilder
    where TDbContext : DbContext
{
    public Func<IEntityType, string> RouteNameFormatter { get; set; }
        = (entityType) =>
        {
            var name = entityType.ClrType.Name;

            if (name.EndsWith("`1") || name.EndsWith("`2") || name.EndsWith("`3"))
                name = name.Substring(0, name.Length - 2);

            return name.Pluralize().Kebaberize();
        };

    public int? AutoGenerateEndpointNestedLevel { get; private set; }


    public DbContextApiBuilder<TDbContext> UseRouteNameGenerationStrategy(Func<IEntityType, string> routeNameFormatter)
    {
        RouteNameFormatter = routeNameFormatter;
        return this;
    }

    public DbContextApiBuilder<TDbContext> AutoGenerateEndpoints(int? nestedLevel = 1)
    {
        AutoGenerateEndpointNestedLevel = nestedLevel;
        return this;
    }

    public void Build(ContainerRegistrationContext containerRegistrationContext)
    {
        using var scope = containerRegistrationContext.RootServiceProvider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<TDbContext>();
        var model = db.Model;
        var attributeSetupedDbEntityTypes = containerRegistrationContext.TypeResolver.DbEntityAttributes
            .Select(x => x.TargetType)
            .Distinct()
            .ToList();

        var entityTypes = db.Model.GetEntityTypes()
            .Where(x => x.FindPrimaryKey() is not null
                && x.FindPrimaryKey()!.Properties.Count == 1) //only support single key entity
            .Where(x => x.ClrType is not null && !attributeSetupedDbEntityTypes.Contains(x.ClrType))
            .ToList();

        foreach (var entityType in entityTypes)
        {
            var primaryKey = entityType.FindPrimaryKey()!.Properties.Single();

            var routeName = RouteNameFormatter(entityType);
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

            //query
            var entityApiQueryRouterType = typeof(IDbEntityApiQueryRouter<,,>)
                .MakeGenericType(typeof(TDbContext), entityType.ClrType, primaryKey.ClrType);

            var memberRouter = (IDbEntityApiQueryRouter<TDbContext>)scope.ServiceProvider
                .GetRequiredService(entityApiQueryRouterType);

            memberRouter.KeyProperty = primaryKey;
            memberRouter.EntityType = entityType;

            memberRouteContext.MemberRouter = memberRouter;
            memberRouter.Register(memberRouteContext);
        }
    }
}
