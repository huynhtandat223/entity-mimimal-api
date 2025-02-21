using CFW.EntityApi.Models.Builders;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace CFW.EntityApi.Models.Deltas;

public class EntityDeltaConverterFactory<TDbContext, TEntity, TKey> : JsonConverterFactory
    where TDbContext : DbContext
    where TEntity : class
{
    public EntityApiConfiguration<TDbContext, TEntity, TKey> EntityApiConfiguration { get; }

    public EntityDeltaConverterFactory(EntityApiConfiguration<TDbContext, TEntity, TKey> entityApiConfiguration)
    {
        EntityApiConfiguration = entityApiConfiguration;
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

        if (argType == EntityApiConfiguration.EntityType)
        {
            var converter = Activator.CreateInstance(conveterType, EntityApiConfiguration.EntityType) as JsonConverter;
            return converter!;
        }

        var navigations = EntityApiConfiguration.DbEntityType!.GetNavigations();
        var navigation = navigations.FirstOrDefault(x => x.ClrType == argType);
        if (navigation != null)
        {
            var converter = Activator.CreateInstance(conveterType, navigation.ForeignKey.PrincipalEntityType) as JsonConverter;
            return converter!;
        }

        var complexProperties = EntityApiConfiguration.DbEntityType!.GetComplexProperties();
        var complexProperty = complexProperties.FirstOrDefault(x => x.ClrType == argType);
        if (complexProperty != null)
        {
            var converter = Activator.CreateInstance(conveterType, complexProperty) as JsonConverter;
            return converter!;
        }

        throw new NotImplementedException();
    }

}
