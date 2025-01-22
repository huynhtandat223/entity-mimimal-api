using CFW.ODataCore.Models.Metadata;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace CFW.ODataCore.Models.Deltas;

public class EntityDeltaConverter<TSource> : JsonConverter<EntityDelta<TSource>>
    where TSource : class
{
    private readonly MetadataEntityProperty _metadataEntityProperty;

    public EntityDeltaConverter(MetadataEntityProperty metadataEntityProperty)
    {
        _metadataEntityProperty = metadataEntityProperty;
    }

    private static string ResolvePropertyName(string propertyName, JsonSerializerOptions options)
    {
        return options.PropertyNamingPolicy?.ConvertName(propertyName) ?? propertyName;
    }

    public override EntityDelta<TSource>? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var delta = new EntityDelta<TSource>();
        delta.ComplexProperty = _metadataEntityProperty.EfCoreComplexProperty;
        delta.EfCoreEntityType = _metadataEntityProperty.EfCoreEntityType;

        var scalarPropertyMap = _metadataEntityProperty.ScalarProperties.ToDictionary(
            p => ResolvePropertyName(p.Name, options),
            p => p,
            StringComparer.OrdinalIgnoreCase
        );

        var complexPropertyMap = _metadataEntityProperty.ComplexProperties.ToDictionary(
            p => ResolvePropertyName(p.Name, options),
            p => p,
            StringComparer.OrdinalIgnoreCase
        );

        var collectionPropertyMap = _metadataEntityProperty.CollectionProperties.ToDictionary(
            p => ResolvePropertyName(p.Name, options),
            p => p,
            StringComparer.OrdinalIgnoreCase
        );

        var entityPropertyMap = _metadataEntityProperty.EntityProperties.ToDictionary(
            p => ResolvePropertyName(p.Name, options),
            p => p,
            StringComparer.OrdinalIgnoreCase
        );

        // Parse the JSON
        if (reader.TokenType != JsonTokenType.StartObject)
            throw new JsonException("Expected a JSON object.");

        while (reader.Read())
        {
            if (reader.TokenType == JsonTokenType.EndObject)
                break;

            if (reader.TokenType != JsonTokenType.PropertyName)
                throw new JsonException("Expected a JSON property.");

            var jsonPropertyName = reader.GetString();
            if (jsonPropertyName.IsNullOrWhiteSpace())
                throw new NotImplementedException();

            // Move to the value
            reader.Read();

            if (scalarPropertyMap.TryGetValue(jsonPropertyName!, out var scalarPropertyInfo))
            {
                var propertyInfo = scalarPropertyInfo!.PropertyInfo;

                var scalarPropertyValue = JsonSerializer.Deserialize(ref reader, propertyInfo!.PropertyType, options);

                propertyInfo.SetValue(delta!.Instance, scalarPropertyValue);
                delta.ChangedProperties[scalarPropertyInfo.Name] = scalarPropertyValue;
                continue;
            }

            if (complexPropertyMap.TryGetValue(jsonPropertyName!, out var complexPropertyInfo))
            {
                //set delta instance
                var complexDeltaType = typeof(EntityDelta<>).MakeGenericType(complexPropertyInfo!.ClrType);
                var complexDelta = JsonSerializer.Deserialize(ref reader, complexDeltaType, options) as EntityDelta;
                complexDelta!.ComplexProperty = complexPropertyInfo;

                delta!.ChangedProperties[complexPropertyInfo.Name] = complexDelta;

                var complexDeltaInstance = complexDelta!.GetType().GetProperty("Instance")!.GetValue(complexDelta);
                complexPropertyInfo.PropertyInfo!.SetValue(delta.Instance, complexDeltaInstance);

                continue;
            }

            if (collectionPropertyMap.TryGetValue(jsonPropertyName!, out var collectionPropertyInfo))
            {
                if (reader.TokenType == JsonTokenType.Null)
                {
                    reader.Skip();
                    continue;
                }

                if (reader.TokenType != JsonTokenType.StartArray)
                    throw new InvalidOperationException("Expected a JSON array.");

                var elementType = collectionPropertyInfo.ForeignKey.DeclaringEntityType.ClrType;

                var deltaArrayType = typeof(EntityDelta<>).MakeGenericType(elementType);
                var deltaSet = new EntityDeltaSet { ObjectType = elementType };
                while (reader.Read() && reader.TokenType != JsonTokenType.EndArray)
                {
                    var elementDelta = JsonSerializer
                        .Deserialize(ref reader, deltaArrayType, options) as EntityDelta;

                    deltaSet.ChangedProperties.Add(elementDelta!);
                }

                delta!.ChangedProperties[collectionPropertyInfo.Name] = deltaSet;

                var list = deltaSet.GetList();
                collectionPropertyInfo.PropertyInfo!.SetValue(delta.Instance, list);

                continue;
            }

            if (entityPropertyMap.TryGetValue(jsonPropertyName!, out var entityPropertyInfo))
            {
                var entityDeltaType = typeof(EntityDelta<>).MakeGenericType(entityPropertyInfo!.ClrType);
                var entityDelta = JsonSerializer.Deserialize(ref reader, entityDeltaType, options) as EntityDelta;
                entityDelta!.EfCoreEntityType = entityPropertyInfo.ForeignKey.DeclaringEntityType;

                delta!.ChangedProperties[entityPropertyInfo.Name] = entityDelta;

                var entityDeltaInstance = entityDelta!.GetType().GetProperty("Instance")!.GetValue(entityDelta);
                entityPropertyInfo.PropertyInfo!.SetValue(delta.Instance, entityDeltaInstance);
                continue;
            }

            //no allow properties
            reader.Skip();
        }

        return delta;
    }

    public override void Write(Utf8JsonWriter writer, EntityDelta<TSource> value, JsonSerializerOptions options)
    {
        throw new NotImplementedException();
    }
}
