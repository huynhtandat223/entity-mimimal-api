using CFW.EntityApi.Models.Builders;
using Microsoft.AspNetCore.Http.Json;
using Microsoft.Extensions.Options;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace CFW.EntityApi.Models.Deltas;

public class EntityDeltaConverterFactory<T> : JsonConverterFactory
{
    public EntityApiConfiguration EntityApiBuilder { get; }

    public EntityDeltaConverterFactory(EntityApiConfiguration entityApiBuilder)
    {
        EntityApiBuilder = entityApiBuilder;
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
        var argType = typeToConvert.GetGenericArguments()[0];

        var conveterType = typeof(EntityDeltaConverter<>).MakeGenericType(argType);

        if (argType == EntityApiBuilder.EntityType)
        {
            var converter = Activator.CreateInstance(conveterType, EntityApiBuilder.DbEntityType) as JsonConverter;
            return converter!;
        }

        var navigations = EntityApiBuilder.DbEntityType!.GetNavigations();
        var navigation = navigations.FirstOrDefault(x => x.ClrType == argType);
        if (navigation != null)
        {
            var converter = Activator.CreateInstance(conveterType, navigation.ForeignKey.PrincipalEntityType) as JsonConverter;
            return converter!;
        }

        var complexProperties = EntityApiBuilder.DbEntityType!.GetComplexProperties();
        var complexProperty = complexProperties.FirstOrDefault(x => x.ClrType == argType);
        if (complexProperty != null)
        {
            var converter = Activator.CreateInstance(conveterType, complexProperty) as JsonConverter;
            return converter!;
        }

        throw new NotImplementedException();
    }

}

public class EntityDelta<TEntity> : EntityDelta
    where TEntity : class
{
    public TEntity? Instance { get; set; } = Activator.CreateInstance<TEntity>();

    public override object? GetInstance() => Instance;

    public static async ValueTask<EntityDelta<TEntity>?> BindAsync(HttpContext context)
    {
        var entityApiBuilder = context.GetEndpoint()?.Metadata.GetMetadata<EntityApiConfiguration>();
        var jsonOptions = context.RequestServices.GetRequiredService<IOptions<JsonOptions>>().Value;

        var customizedOptions = new JsonSerializerOptions(jsonOptions.SerializerOptions);
        var factory = new EntityDeltaConverterFactory<TEntity>(entityApiBuilder!);

        customizedOptions.Converters.Add(factory);

        var delta = await JsonSerializer.DeserializeAsync<EntityDelta<TEntity>>(context.Request.Body
            , customizedOptions);

        return delta;
    }


}
