using CFW.EntityApi.Deletions;
using CFW.EntityApi.Models.Deltas;
using CFW.EntityApi.Queries;
using Microsoft.AspNetCore.OData.Query;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

namespace CFW.EntityApi.Models.Builders;

public class EntityApiContextBuilder
{
    internal List<EntityApiConfiguration> EntityApiConfigurations { get; } = new List<EntityApiConfiguration>();

    public EntityApiBuilder<T> ConfigureEntity<T>()
        where T : class
    {
        var apiBuilder = new EntityApiBuilder<T>();
        EntityApiConfigurations.Add(apiBuilder.Configuration);

        return apiBuilder;
    }
}

public class EntityApiConfiguration
{
    internal Type EntityType { get; set; }

    internal AllowedQueryOptions? AllowedQueryOptions { get; set; }

    internal string? RouteName { get; set; }

    internal Func<IServiceProvider, EntityDelta, Task<Result>>? CreationFactory { get; set; }

    public EntityApiConfiguration(Type entityType)
    {
        EntityType = entityType;
    }

    public Type? DbContextType { get; internal set; }

    internal IEntityType? DbEntityType { get; }

    internal IProperty? DbPrimaryKey { get; }

    public EntityApiConfiguration(IEntityType dbEntityType, IProperty primaryKey)
    {
        DbEntityType = dbEntityType;
        EntityType = dbEntityType.ClrType;
        DbPrimaryKey = primaryKey;
    }
}

public class EntityApiConfiguration<TEntity, TKey> : EntityApiConfiguration<TEntity>
    where TEntity : class
{
    public EntityApiConfiguration(IEntityType dbEntityType, IProperty primaryKey)
        : base(dbEntityType, primaryKey)
    {
    }

    internal Func<IServiceProvider, Task<IEntityApiDeletionHandler<TEntity, TKey>>>? DeletionHandlerFactory { get; set; }
}

public class DbEntityApiConfiguration<TDbContext, TEntity, TKey> : EntityApiConfiguration<TEntity, TKey>
    where TDbContext : DbContext
    where TEntity : class
{
    public DbEntityApiConfiguration(IEntityType dbEntityType, IProperty primaryKey)
        : base(dbEntityType, primaryKey)
    {
        DbContextType = typeof(TDbContext);
    }
}

public class EntityApiConfiguration<TEntity> : EntityApiConfiguration
    where TEntity : class
{
    public EntityApiConfiguration()
        : base(typeof(TEntity))
    {
    }

    public EntityApiConfiguration(IEntityType dbEntityType, IProperty primaryKey)
        : base(dbEntityType, primaryKey)
    {
    }

    internal Func<IServiceProvider, Task<IEntityApiQuery<TEntity>>>? QueryFactory { get; set; }
}

public class EntityApiBuilder<T>
    where T : class
{
    public EntityApiConfiguration<T> Configuration { get; }

    public EntityApiBuilder()
    {
        Configuration = new EntityApiConfiguration<T>();
    }

    public EntityApiBuilder<T> ConfigureAllowQueryOptions(AllowedQueryOptions allowedQueryOptions)
    {
        Configuration.AllowedQueryOptions = allowedQueryOptions;
        return this;
    }

    public EntityApiBuilder<T> UseRouteName(string routeName)
    {
        Configuration.RouteName = routeName;
        return this;
    }

    public EntityApiBuilder<T> UseQueryFactory<TService>(Func<TService, Task<IQueryable<T>>> queryableGetter)
        where TService : class
    {
        Configuration.QueryFactory = async (s) =>
        {
            var service = s.GetRequiredService<TService>();
            var queryable = await queryableGetter(service);
            return new DefaultEntityApiQuery<T>(queryable);
        };
        return this;
    }

    public EntityApiBuilder<T> UseCreationFactory<TService>(Func<TService, EntityDelta<T>, Task<Result>>
        creationFactory)
        where TService : class
    {
        Configuration.CreationFactory = (s, delta) =>
        {
            if (delta is not EntityDelta<T> entityDelta)
            {
                throw new InvalidOperationException("Invalid delta type");
            }

            var service = s.GetRequiredService<TService>();
            return creationFactory(service, entityDelta);
        };
        return this;
    }
}
