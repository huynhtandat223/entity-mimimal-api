using CFW.Core.Utils;
using CFW.EntityApi.Queries;
using CFW.EntityApi.Registrators;
using Humanizer;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.AspNetCore.Mvc.Formatters;
using Microsoft.AspNetCore.OData.Formatter;
using Microsoft.AspNetCore.OData.Query;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Metadata.Internal;

namespace CFW.EntityApi.Models.Builders;

public class EntityFrameworkPopuplationFeature<TDbContext> : IApiFeature
    where TDbContext : DbContext
{
    private readonly ContainerConfiguration _containerConfiguration;

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

    internal Func<IEntityType, bool> EntitiesSelector { get; set; }

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

    public async Task Register(ContainerRegistrationContext containerRegistrationContext)
    {
        using var scope = containerRegistrationContext.RootServiceProvider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<TDbContext>();
        var model = db.Model;

        var entityActions = containerRegistrationContext.TypeResolver.EntityActionAttributes;

        var entityTypes = db.Model.GetEntityTypes()
            .Where(x => x.FindPrimaryKey() is not null
                && x.FindPrimaryKey()!.Properties.Count == 1) //only support single key entity
            .Where(EntitiesSelector)
            .ToList();


        foreach (var entityType in entityTypes)
        {
            var primaryKey = entityType.FindPrimaryKey()!.Properties.Single();

            var configuredEntity = containerRegistrationContext.TypeResolver.ConfiguredEntities
                .FirstOrDefault(x => x.EntityType == entityType.ClrType);
            if (configuredEntity is null)
            {
                var apiConfigurationType = typeof(DbEntityApiConfiguration<,,>)
                    .MakeGenericType(typeof(TDbContext), entityType.ClrType, primaryKey.ClrType);

                configuredEntity = (EntityApiConfiguration)Activator
                    .CreateInstance(apiConfigurationType, entityType, primaryKey)!;
            }

            var routeName = configuredEntity?.RouteName ?? _routeNameFormater(entityType);

            var router = containerRegistrationContext.ContainerGroupRoute
                    .MapGroup(routeName)
                    .WithMetadata(configuredEntity!);

            var memberRouteContext = new ContainerMemberRegistrationContext
            {
                ContainerRegistrationContext = containerRegistrationContext,
                MemberRouteGroup = router,
                Name = routeName,
                EntityConfiguration = configuredEntity!
            };



            //query
            var entityApiQueryRouterType = typeof(IDbEntityApiQueryRouter<,,>)
            .MakeGenericType(typeof(TDbContext), entityType.ClrType, primaryKey.ClrType);

            var memberRouter = (IDbEntityApiQueryRouter<TDbContext>)scope.ServiceProvider
                .GetRequiredService(entityApiQueryRouterType);

            memberRouter.KeyProperty = primaryKey;
            memberRouter.EntityType = entityType;
            memberRouter.EntityApiBuilder = configuredEntity;

            memberRouteContext.QueryMemberRouter = memberRouter;
            await memberRouter.Register(memberRouteContext);

            //creation
            var entityApiCreationRouterType = typeof(IDbEntityApiCreationRouter<,,>)
                .MakeGenericType(typeof(TDbContext), entityType.ClrType, primaryKey.ClrType);

            var creationRouter = (IDbEntityApiCreationRouter<TDbContext>)scope.ServiceProvider
                .GetRequiredService(entityApiCreationRouterType);

            creationRouter.KeyProperty = primaryKey;
            creationRouter.EntityType = entityType;
            creationRouter.EntityApiConfiguration = configuredEntity;

            memberRouteContext.CreationMemberRouter = creationRouter;
            await creationRouter.Register(memberRouteContext);

            //deletion
            var entityApiDeletionRouterType = typeof(Deletions.Router<,>)
                .MakeGenericType(entityType.ClrType, primaryKey.ClrType);
            var deletionRouter = (IContainerMemberRouter)ActivatorUtilities
                .CreateInstance(scope.ServiceProvider, entityApiDeletionRouterType);

            await deletionRouter.Register(memberRouteContext);

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
    }

    private static void RegisterQueryEndpoint(RouteGroupBuilder entityGroupBuider
        , EntityApiConfiguration entityApiConfiguration
        , ContainerRegistrationContext containerRegistrationContext)
    {
        //var entityConfiguration = (EntityApiConfiguration<TEntity>)containerMemberRegistrationContext.EntityConfiguration;

        var allowQueryOptions = entityApiConfiguration.AllowedQueryOptions
            ?? containerRegistrationContext.ContainerConfiguration.AllowedQueryOptions;

        var ignoreQueryOptions = ~allowQueryOptions;

        var entityApiQueryFactory = entityConfiguration?.QueryFactory;
        if (entityApiQueryFactory is null)
        {
            entityApiQueryFactory = s =>
            {
                var db = s.GetRequiredService<TDbContext>();
                var queryable = db.Set<TEntity>().AsNoTracking().AsQueryable();

                var result = new DefaultEntityApiQuery<TEntity>(queryable);

                return Task.FromResult<IEntityApiQuery<TEntity>>(result);
            };
        }

        entityGroupBuider.MapGet("/", async (ODataOutputFormatter outputFormatter
        , HttpContext httpContext
        , ODataOutputFormatter formatter
        , CancellationToken cancellationToken) =>
        {
            var odataFeature = containerMemberRegistrationContext.ODataFeature;
            if (odataFeature is null)
            {
                odataFeature = containerMemberRegistrationContext
                .CreateODataFeature<TEntity>(httpContext.RequestServices
                    , EntityType, KeyProperty.PropertyInfo!);
            }

            httpContext.Features.Set(odataFeature);

            var entityApiQuery = await entityApiQueryFactory(httpContext.RequestServices);

            var queryBuilder = new QueryBuilder(httpContext.Request.Query);
            var maxTop = containerMemberRegistrationContext.ODataOptions.QueryConfigurations.MaxTop;

            //Maybe $top always support by Odata
            var availableTop = new string[] { "$top", "top" };

            var topQuery = httpContext.Request.Query.SingleOrDefault(x => availableTop.Contains(x.Key.ToLower().Trim()));

            if (topQuery.Key.IsNullOrWhiteSpace())
            {
                queryBuilder.Add("$top", maxTop!.Value.ToString());
            }
            else
            {
                if (!int.TryParse(topQuery.Value, out var topValue))
                {
                    queryBuilder.Add("$top", maxTop!.Value.ToString());
                }
                else if (topValue > maxTop!.Value)
                {
                    queryBuilder.Add("$top", maxTop!.Value.ToString());
                }
            }

            httpContext.Request.QueryString = queryBuilder.ToQueryString();

            var odataQueryContext = new ODataQueryContext(odataFeature.Model, typeof(TEntity), odataFeature.Path);
            var options = new ODataQueryOptions<TEntity>(odataQueryContext, httpContext.Request);

            var result = await entityApiQuery.ExecuteQuery(options, cancellationToken);

            var formatterContext = new OutputFormatterWriteContext(httpContext,
                (stream, encoding) => new StreamWriter(stream, encoding),
                result.GetType() ?? typeof(object), result)
            {
                ContentType = "application/json;odata.metadata=none",
            };

            await formatter.WriteAsync(formatterContext);

        }).WithMetadata(containerMemberRegistrationContext);

        return Task.CompletedTask;
    }
}
