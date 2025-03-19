using CFW.Core.Utils;
using CFW.EntityApi.Models.Deltas;
using CFW.EntityApi.Registrators;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Formatters;
using Microsoft.AspNetCore.OData.Formatter;
using Microsoft.AspNetCore.OData.Query;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata;
using System.Text.Json.Serialization;

namespace CFW.EntityApi.Models.Builders;

public class EntityApiConfigurationBuilder<TEntity>
    where TEntity : class
{
    private readonly EntityApiConfiguration<TEntity> _entityApiConfiguration;
    internal EntityApiConfigurationBuilder(EntityApiConfiguration<TEntity> entityApiConfiguration)
    {
        _entityApiConfiguration = entityApiConfiguration;
    }

    public EntityApiConfigurationBuilder<TEntity> UseName(string routeName)
    {
        _entityApiConfiguration.RouteName = routeName;
        return this;
    }

    public EntityApiConfigurationBuilder<TEntity> UseQueryAsync<TService>(Func<TService
        , ODataQueryOptions<TEntity>, Task<IQueryable>> queryFunc)
        where TService : class
    {
        _entityApiConfiguration.QueryFactory = async (s, options) =>
        {
            var service = s.GetRequiredService<TService>()!;
            var entityOptions = (ODataQueryOptions<TEntity>)options;
            var query = await queryFunc(service, entityOptions);
            return query.Success();
        };
        return this;
    }

    public EntityApiConfigurationBuilder<TEntity> UseQuery<TService>(Func<TService
        , ODataQueryOptions<TEntity>, IQueryable> queryFunc)
        where TService : class
    {
        _entityApiConfiguration.QueryFactory = (s, options) =>
        {
            var service = s.GetRequiredService<TService>()!;
            var entityOptions = (ODataQueryOptions<TEntity>)options;
            var query = queryFunc(service, entityOptions);
            var result = query.Success();

            return Task.FromResult(result);
        };
        return this;
    }

    public EntityApiConfigurationBuilder<TEntity> UseQuery<TService>(Func<TService
        , IQueryable> queryFunc)
        where TService : class
    {
        _entityApiConfiguration.QueryFactory = (s, _) =>
        {
            var service = s.GetRequiredService<TService>()!;
            var query = queryFunc(service);
            var result = query.Success();

            return Task.FromResult(result);
        };
        return this;
    }
}

public abstract class EntityApiConfiguration
{
    internal Type EntityType { get; set; } = null!;

    internal AllowedQueryOptions? AllowedQueryOptions { get; set; }

    internal string? RouteName { get; set; }

    internal Func<IServiceProvider, EntityDelta, Task<Result>>? CreationFactory { get; set; }

    internal Func<IServiceProvider, ODataQueryOptions, Task<Result<IQueryable>>>? QueryFactory { get; set; }

    internal Func<IServiceProvider, object, Task<Result>>? DeletionExecutor { get; set; }

    public Type? DbContextType { get; internal set; }

    public virtual Task RegisterRoutes(ContainerMemberRegistrationContext registrationContext) => Task.CompletedTask;

    public virtual Task Initialize(IServiceProvider serviceProvider) => Task.CompletedTask;

    public virtual JsonConverterFactory? GetDeltaConverterFactory() => null;

    public EntityApiConfiguration(Type entityType)
    {
        EntityType = entityType;
    }

    public EntityApiConfiguration()
    {
    }
}

public class EntityApiConfiguration<TEntity> : EntityApiConfiguration
    where TEntity : class
{
    public EntityApiConfiguration() { }

    public EntityApiConfiguration(IEntityApiConfiguration<TEntity> entityApiConfiguration)
    {
        EntityType = typeof(TEntity);
        var builder = new EntityApiConfigurationBuilder<TEntity>(this);

        entityApiConfiguration.Configure(builder);
    }

    public override Task RegisterRoutes(ContainerMemberRegistrationContext registrationContext)
    {
        var entityGroupBuider = registrationContext.MemberRouteGroup;
        var containerRegistrationContext = registrationContext.ContainerRegistrationContext;

        if (CreationFactory is not null)
        {
            entityGroupBuider.MapPost("/", async (EntityDelta<TEntity> delta
            , [FromServices] IServiceProvider sp
            , CancellationToken cancellationToken) =>
            {
                var result = await CreationFactory(sp, delta);
                return result.ToResults();
            });
        }

        if (QueryFactory is not null)
        {
            entityGroupBuider.MapGet("/", async (ODataOutputFormatter outputFormatter
            , HttpContext httpContext
            , ODataOutputFormatter formatter
            , CancellationToken cancellationToken) =>
            {
                var odataFeature = registrationContext.ODataFeature;
                if (odataFeature is null)
                {
                    odataFeature = registrationContext
                    .CreateODataFeature<TEntity>(httpContext.RequestServices
                        , null, null);
                }
                httpContext.Features.Set(odataFeature);
                var queryBuilder = new QueryBuilder(httpContext.Request.Query);
                var maxTop = registrationContext.ODataOptions.QueryConfigurations.MaxTop;
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
                var queryResult = await QueryFactory(httpContext.RequestServices, options);
                var result = queryResult.Data!;
                var formatterContext = new OutputFormatterWriteContext(httpContext,
                    (stream, encoding) => new StreamWriter(stream, encoding),
                    result.GetType() ?? typeof(object), result)
                {
                    ContentType = "application/json;odata.metadata=none",
                };
                await formatter.WriteAsync(formatterContext);
            }).WithMetadata(registrationContext);
        }

        if (DeletionExecutor is not null)
        {
            entityGroupBuider.MapDelete("/{key}", async (TEntity key
                , [FromServices] IServiceProvider sp
                , CancellationToken cancellationToken) =>
            {
                var result = await DeletionExecutor(sp, key!);
                return result.ToResults();
            });
        }

        return Task.CompletedTask;
    }

}

public class EntityApiConfiguration<TDbContext, TEntity> : EntityApiConfiguration<TEntity>
    where TDbContext : DbContext
    where TEntity : class
{
    public EntityApiConfiguration(IEntityType dbEntityType)
    {
        if (dbEntityType.ClrType != typeof(TEntity))
        {
            throw new InvalidOperationException();
        }

        DbContextType = typeof(TDbContext);
        DbEntityType = dbEntityType;
        EntityType = dbEntityType.ClrType;
    }

    internal IEntityType? DbEntityType { get; }
}

public class EntityApiConfiguration<TDbContext, TEntity, TKey> : EntityApiConfiguration<TDbContext, TEntity>
    where TDbContext : DbContext
    where TEntity : class
{
    public EntityApiConfiguration(IEntityType dbEntityType, IProperty primaryKey)
        : base(dbEntityType)
    {
        DbContextType = typeof(TDbContext);
        DbPrimaryKey = primaryKey;
    }

    internal IProperty DbPrimaryKey { get; }

    public override JsonConverterFactory? GetDeltaConverterFactory()
    {
        return new EntityDeltaConverterFactory<TDbContext, TEntity, TKey>(this);
    }

    public override async Task RegisterRoutes(ContainerMemberRegistrationContext registrationContext)
    {
        var entityGroupBuider = registrationContext.MemberRouteGroup;
        var containerRegistrationContext = registrationContext.ContainerRegistrationContext;

        var serviceProvider = containerRegistrationContext.RootServiceProvider;
        var customConfiguration = serviceProvider.GetService<IEntityApiConfiguration<TEntity>>();
        if (customConfiguration is not null)
        {
            await customConfiguration.Configure(new EntityApiConfigurationBuilder<TEntity>(this));
        }

        RegisterCreationRoute(entityGroupBuider);

        RegisterQueryRoute(entityGroupBuider, registrationContext, containerRegistrationContext);

        RegisterDeleteRoute(entityGroupBuider);
    }

    private void RegisterDeleteRoute(RouteGroupBuilder routeGroupBuilder)
    {
        DeletionExecutor = async (s, key) =>
        {
            var db = s.GetRequiredService<TDbContext>();
            var entity = await db.Set<TEntity>().FindAsync(new object[] { key });
            if (entity is null)
            {
                return entity.Failed("Entity not found");
            }

            db.Set<TEntity>().Remove(entity);
            var affected = await db.SaveChangesAsync();
            if (affected == 0)
            {
                return entity.Failed("Failed to delete entity");
            }
            return entity.Success();
        };

        routeGroupBuilder.MapDelete("/{key}", async (TKey key
            , [FromServices] IServiceProvider sp
            , CancellationToken cancellationToken) =>
        {
            var result = await DeletionExecutor(sp, key!);
            return result.ToResults();
        });
    }

    private void RegisterQueryRoute(RouteGroupBuilder routeGroupBuilder
        , ContainerMemberRegistrationContext registrationContext
        , ContainerRegistrationContext containerRegistrationContext)
    {
        var allowQueryOptions = AllowedQueryOptions
            ?? containerRegistrationContext.ContainerConfiguration.AllowedQueryOptions;

        var ignoreQueryOptions = ~allowQueryOptions;

        if (QueryFactory is null)
        {
            QueryFactory = (s, options) =>
            {
                var db = s.GetRequiredService<TDbContext>();
                var queryable = db.Set<TEntity>().AsNoTracking().AsQueryable();

                var result = options.ApplyTo(queryable, ignoreQueryOptions);

                return Task.FromResult(result.Success());
            };
        }

        routeGroupBuilder.MapGet("/", async (ODataOutputFormatter outputFormatter
        , HttpContext httpContext
        , ODataOutputFormatter formatter
        , CancellationToken cancellationToken) =>
        {
            var odataFeature = registrationContext.ODataFeature;
            if (odataFeature is null)
            {
                odataFeature = registrationContext
                .CreateODataFeature<TEntity>(httpContext.RequestServices
                    , DbEntityType, DbPrimaryKey.PropertyInfo);
            }

            httpContext.Features.Set(odataFeature);

            var queryBuilder = new QueryBuilder(httpContext.Request.Query);
            var maxTop = registrationContext.ODataOptions.QueryConfigurations.MaxTop;

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

            var queryResult = await QueryFactory(httpContext.RequestServices, options);
            var result = queryResult.Data!;

            var formatterContext = new OutputFormatterWriteContext(httpContext,
                (stream, encoding) => new StreamWriter(stream, encoding),
                result.GetType() ?? typeof(object), result)
            {
                ContentType = "application/json;odata.metadata=none",
            };

            await formatter.WriteAsync(formatterContext);

        }).WithMetadata(registrationContext);

    }

    private void RegisterCreationRoute(RouteGroupBuilder routeGroupBuilder)
    {
        CreationFactory = async (sp, delta) =>
        {
            if (delta is not EntityDelta<TEntity> entityDelta)
            {
                throw new InvalidOperationException();
            }

            var db = sp.GetRequiredService<TDbContext>();
            var entity = entityDelta.Instance!;
            var entry = db!.Set<TEntity>().Add(entity);

            await ProcessChangedNavigationPropertiesRecursive(delta.ChangedProperties!, entry, default);

            var affected = await db.SaveChangesAsync();
            if (affected == 0)
            {
                return entity.Failed("Failed to create entity");
            }

            return entity.Created();
        };

        routeGroupBuilder.MapPost("/", async (EntityDelta<TEntity> delta
            , [FromServices] IServiceProvider sp
            , CancellationToken cancellationToken) =>
        {
            var result = await CreationFactory(sp, delta);
            return result.ToResults();
        });
    }

    private async Task ProcessChangedNavigationPropertiesRecursive(
        IDictionary<string, object> changedProperties,
        EntityEntry entry,
        CancellationToken cancellationToken = default)
    {
        var entityDeltas = changedProperties
            .Where(x => x.Value is EntityDelta delta && delta.EfCoreEntityType is not null);

        foreach (var (key, value) in entityDeltas)
        {
            var delta = (EntityDelta)value;
            var navigation = entry.Navigation(key);
            if (!navigation.IsLoaded)
            {
                await navigation.LoadAsync(cancellationToken);
            }
            await ProcessChangedNavigationPropertiesRecursive(delta.ChangedProperties!
                , entry.Context.Entry(navigation.CurrentValue!), cancellationToken);
        }

        var collectionDeltas = changedProperties
            .Where(x => x.Value is EntityDeltaSet deltaSets);

        foreach (var (key, value) in collectionDeltas)
        {
            var deltaSets = (EntityDeltaSet)value;
            var navigation = entry.Navigation(key);
            if (!navigation.IsLoaded)
            {
                await navigation.LoadAsync(cancellationToken);
            }

            foreach (var delta in deltaSets.ChangedProperties)
            {
                var itemEntry = entry.Context.Entry(delta.GetInstance()!);
                await ProcessChangedNavigationPropertiesRecursive(delta!.ChangedProperties!
                    , itemEntry, cancellationToken);
            }
        }
    }
}
