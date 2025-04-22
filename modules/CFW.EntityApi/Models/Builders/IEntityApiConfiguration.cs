using CFW.EntityApi.Attributes;
using CFW.EntityApi.Models.Deltas;
using Humanizer;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata;

namespace CFW.EntityApi.Models.Builders;

public interface IEntityApiConfiguration<TEntity>
    where TEntity : class
{
    Task Configure(EntityApiConfigurationBuilder<TEntity> builder);
}

public abstract class DbEntityApiConfiguration<TEntity, TDbContext, TKey> : IEntityApiConfiguration<TEntity>
    where TEntity : class
    where TDbContext : DbContext
{
    private readonly TDbContext _db;
    public DbEntityApiConfiguration(TDbContext db)
    {
        _db = db;
    }

    public virtual string RouteName => typeof(TEntity).Name.Pluralize().Camelize();

    public virtual Task Configure(EntityApiConfigurationBuilder<TEntity> builder)
    {
        var dbEntityType = _db.Model.FindEntityType(typeof(TEntity));
        var properties = GetPropertiesRecursive(dbEntityType!);

        builder.UseProperties(properties);

        builder.UseName(RouteName)
            .UseQuery<TDbContext>(db => db.Set<TEntity>().AsNoTracking())
            .UseCreation<TDbContext, TKey>(CreateEntity);

        return Task.CompletedTask;
    }

    private static List<ApiProperty> GetPropertiesRecursive(IEntityType entityType, int depth = 0)
    {
        if (depth > 2) return new List<ApiProperty>();

        var properties = new List<ApiProperty>();

        foreach (var prop in entityType.GetProperties())
        {
            properties.Add(new ApiProperty
            {
                Name = prop.Name,
                ClrType = prop.ClrType,
                IsRequired = !prop.IsNullable,
                Type = ApiPropertyType.Scalar
            });
        }

        foreach (var complex in entityType.GetComplexProperties())
        {
            properties.Add(new ApiProperty
            {
                Name = complex.Name,
                ClrType = complex.ClrType,
                IsRequired = false,
                Type = ApiPropertyType.Complex,
                NestedProperties = Enumerable.Empty<ApiProperty>()
            });
        }

        foreach (var nav in entityType.GetNavigations())
        {
            properties.Add(new ApiProperty
            {
                Name = nav.Name,
                ClrType = nav.ClrType,
                IsRequired = false,
                Type = nav.IsCollection ? ApiPropertyType.Collection : ApiPropertyType.Complex,
                NestedProperties = GetPropertiesRecursive(nav.TargetEntityType, depth + 1)
            });
        }

        return properties;
    }



    public virtual Task<Result> PostCreate(EntityDelta<TEntity> entityDelta)
    {
        return Task.FromResult(new Result { IsSuccess = true });
    }

    private async Task<Result> CreateEntity(IServiceProvider serviceProvider, EntityDelta delta)
    {
        if (delta is not EntityDelta<TEntity> entityDelta)
        {
            return this.Failed("Invalid delta type");
        }

        var postCreateResult = await PostCreate(entityDelta);
        if (postCreateResult.IsNotSuccess())
        {
            return postCreateResult;
        }

        var db = serviceProvider.GetRequiredService<TDbContext>();
        var entity = entityDelta.Instance!;
        var entry = db!.Set<TEntity>().Add(entity);

        await ProcessChangedNavigationPropertiesRecursive(entityDelta.ChangedProperties!, entry, default);

        var affected = await db.SaveChangesAsync();
        if (affected == 0)
        {
            return entity.Failed("Failed to create entity");
        }

        return entity.Created();
    }

    private static async Task ProcessChangedNavigationPropertiesRecursive(
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

public class AttributeEntityApiConfiguration<TEntity> : IEntityApiConfiguration<TEntity>
    where TEntity : class
{
    private readonly EntityAttribute _entityAttribute;
    public AttributeEntityApiConfiguration(EntityAttribute entityAttribute)
    {
        _entityAttribute = entityAttribute;
    }
    public Task Configure(EntityApiConfigurationBuilder<TEntity> builder)
    {
        var name = _entityAttribute.Name ?? typeof(TEntity).Name;

        builder
            .UseName(name)
            .UseQuery<DefaultDbContext>(db => db.Set<TEntity>().AsQueryable());

        return Task.CompletedTask;
    }
}
