using CFW.ODataCore.Models.Deltas;
using CFW.ODataCore.Models.Metadata;
using Microsoft.AspNetCore.OData.Query;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata;
using System.Linq.Expressions;
using System.Text.Json.Serialization;

namespace CFW.ODataCore.Models;

public class EntityEndpoint
{
    public AllowedQueryOptions? AllowedQueryOptions { get; set; }
}

public class EntityEndpoint<TEntity> : EntityEndpoint
    where TEntity : class
{
    public virtual Expression<Func<TEntity, TEntity>> Model { get; } = x => x;

    private MetadataEntity? _metadata;

    internal void SetMetadata(MetadataEntity metadata)
    {
        _metadata = metadata;
    }

    internal JsonConverter GetConverter(Type typeToConvert)
    {
        var conveterType = typeof(EntityDeltaConverter<>).MakeGenericType(typeToConvert);
        var metadataEntityProperty = FindMetadataProperty(typeToConvert);

        var converter = Activator.CreateInstance(conveterType, metadataEntityProperty) as JsonConverter;

        return converter!;
    }

    public MetadataEntityProperty FindMetadata()
    {
        return FindMetadataProperty(typeof(TEntity));
    }

    private MetadataEntityProperty FindMetadataProperty(Type typeToConvert)
    {
        var entityType = _metadata!.SupportEntities.FirstOrDefault(x => x.ClrType == typeToConvert);
        if (entityType is not null)
        {
            return FindMetadataEntityProperty(entityType);
        }

        var navigationProperty = _metadata!.SupportEntities.SelectMany(x => x.GetNavigations())
            .FirstOrDefault(x => x.ClrType == typeToConvert);

        if (navigationProperty is not null)
        {
            var navigationEntityType = navigationProperty.ForeignKey.PrincipalEntityType;
            return FindMetadataEntityProperty(navigationEntityType);
        }

        var complexProperty = _metadata!.SupportEntities.SelectMany(x => x.GetComplexProperties())
            .FirstOrDefault(x => x.ClrType == typeToConvert);
        if (complexProperty is not null)
        {
            return FindMetadataComplexProperty(complexProperty);
        }

        throw new InvalidOperationException($"Type {typeToConvert} not found in metadata");
    }

    private MetadataEntityProperty FindMetadataComplexProperty(IComplexProperty complexProperty)
    {
        return new MetadataEntityProperty
        {
            EfCoreComplexProperty = complexProperty,
            ScalarProperties = complexProperty.ComplexType.GetProperties(),
            CollectionProperties = Enumerable.Empty<INavigation>(),
            EntityProperties = Enumerable.Empty<INavigation>(),
            ComplexProperties = Enumerable.Empty<IComplexProperty>()
        };
    }

    private MetadataEntityProperty FindMetadataEntityProperty(IEntityType efCoreEntityType)
    {
        var scalarProperties = efCoreEntityType.GetProperties();
        var navigationProperties = efCoreEntityType.GetNavigations();

        var entityProperties = navigationProperties
            .Where(x => !x.IsCollection)
            .ToArray();

        var collectionProperties = navigationProperties
            .Where(x => x.IsCollection)
            .ToArray();

        var complexProperties = efCoreEntityType.GetComplexProperties()
            .ToArray();

        return new MetadataEntityProperty
        {
            EfCoreEntityType = efCoreEntityType,
            ScalarProperties = scalarProperties,
            CollectionProperties = collectionProperties,
            EntityProperties = entityProperties,
            ComplexProperties = complexProperties
        };
    }

    internal async Task<IQueryable<TEntity>> GetQueryable(IServiceProvider serviceProvider)
    {
        var serviceKey = _metadata.Container.RoutePrefix;
        var dbContextProvider = serviceProvider.GetRequiredKeyedService<IDbContextProvider>(serviceKey);
        var db = dbContextProvider.GetDbContext();
        var queryable = db.Set<TEntity>().AsNoTracking();

        return await Task.FromResult(queryable);
    }


    internal async Task<Result> CreateEntity(EntityDelta<TEntity> delta
        , IServiceProvider serviceProvider, CancellationToken cancellationToken)
    {
        var serviceKey = _metadata.Container.RoutePrefix;
        var dbContextProvider = serviceProvider.GetRequiredKeyedService<IDbContextProvider>(serviceKey);
        var db = dbContextProvider.GetDbContext();
        var entity = delta.Instance!;

        var entry = db.Set<TEntity>().Add(entity);

        await ProcessChangedNavigationPropertiesRecursive(delta.ChangedProperties!, entry, cancellationToken);

        var affected = await db.SaveChangesAsync(cancellationToken);
        if (affected == 0)
        {
            return entity.Failed("Failed to create entity");
        }

        return entity.Created();
    }

    internal async Task<Result> PatchEntity<TKey>(EntityDelta<TEntity> delta
        , TKey key
        , IServiceProvider serviceProvider, CancellationToken cancellationToken)
    {
        var serviceKey = _metadata.Container.RoutePrefix;
        var dbContextProvider = serviceProvider.GetRequiredKeyedService<IDbContextProvider>(serviceKey);
        var db = dbContextProvider.GetDbContext();

        var dbEntity = await db.FindAsync<TEntity>([key], cancellationToken);
        if (dbEntity is null)
        {
            return delta.Notfound();
        }

        var dbEntry = db.Entry(dbEntity!);
        await ProcessChangedNavigationPropertiesRecursive(delta.ChangedProperties!, dbEntry, cancellationToken);
        dbEntry.CurrentValues.SetValues(delta.Instance!);

        var affected = await db.SaveChangesAsync(cancellationToken);
        if (affected == 0)
        {
            return dbEntity.Failed("Failed to create entity");
        }

        return dbEntity.Ok();
    }

    private async Task ProcessChangedNavigationPropertiesRecursive(
        IDictionary<string, object> changedProperties,
        EntityEntry entry,
        CancellationToken cancellationToken)
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

    internal async Task<Result> DeleteEntity<TKey>(TKey? key, IServiceProvider serviceProvider, CancellationToken cancellationToken)
    {
        var serviceKey = _metadata.Container.RoutePrefix;
        var dbContextProvider = serviceProvider.GetRequiredKeyedService<IDbContextProvider>(serviceKey);
        var db = dbContextProvider.GetDbContext();
        var dbEntity = await db.FindAsync<TEntity>(key, cancellationToken);
        if (dbEntity is null)
        {
            return dbEntity.Notfound();
        }
        db.Remove(dbEntity);
        var affected = await db.SaveChangesAsync(cancellationToken);
        if (affected == 0)
        {
            return dbEntity.Failed("Failed to delete entity");
        }
        return dbEntity.Ok();
    }
}
