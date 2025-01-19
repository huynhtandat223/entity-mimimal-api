using CFW.ODataCore.Models.Deltas;
using CFW.ODataCore.Models.Metadata;
using Microsoft.EntityFrameworkCore.Metadata;
using System.Linq.Expressions;
using System.Text.Json.Serialization;

namespace CFW.ODataCore.Models;

public class EntityEndpoint<TEntity>
    where TEntity : class, new()
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

        var converter = Activator.CreateInstance(conveterType, metadataEntityProperty);

        return (JsonConverter)converter!;
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
}
