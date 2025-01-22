using System.Text.Json;
using System.Text.Json.Serialization;

namespace CFW.ODataCore.Models.Deltas;

public class DeltaConverterFactory<TEntity> : JsonConverterFactory
    where TEntity : class
{
    private readonly EntityEndpoint<TEntity> _configuration;

    public DeltaConverterFactory(EntityEndpoint<TEntity> configuration)
    {
        _configuration = configuration;
    }

    public override bool CanConvert(Type typeToConvert)
    {
        if (!typeToConvert.IsGenericType)
            return false;

        var genericType = typeToConvert.GetGenericTypeDefinition();
        return genericType == typeof(EntityDelta<>);
    }

    public override JsonConverter? CreateConverter(Type typeToConvert, JsonSerializerOptions options)
    {
        var entityType = typeToConvert.GetGenericArguments()[0];
        var conveter = _configuration.GetConverter(entityType);
        return conveter;
    }
}
