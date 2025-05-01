using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace CFW.DynamicApi.Deltas;
public class EntityDeltaConverterFactory<TEntity, TDbContext> : JsonConverterFactory
    where TEntity : class
    where TDbContext : DbContext
{
    private readonly TDbContext _db;

    public EntityDeltaConverterFactory(TDbContext db)
    {
        _db = db;
    }

    private List<IEntityType> _navigations = new List<IEntityType>();


    public override bool CanConvert(Type typeToConvert)
    {
        if (!typeToConvert.IsGenericType)
            return false;

        var genericType = typeToConvert.GetGenericTypeDefinition();
        return genericType == typeof(EntityDelta<>);
    }

    public override JsonConverter? CreateConverter(Type typeToConvert, JsonSerializerOptions options)
    {
        var dbEntityType = _db.Set<TEntity>().EntityType;
        var argType = typeToConvert.GetGenericArguments()[0];
        if (argType == typeof(TEntity))
            return new EntityDeltaConverter<TEntity>(dbEntityType);

        var conveterType = typeof(EntityDeltaConverter<>).MakeGenericType(argType);
        var navigations = dbEntityType.GetNavigations();
        var navigation = navigations.FirstOrDefault(x => x.ClrType == argType);
        if (navigation != null)
        {
            if (!_navigations.Contains(navigation.ForeignKey.PrincipalEntityType))
                _navigations.Add(navigation.ForeignKey.PrincipalEntityType);

            var converter = Activator.CreateInstance(conveterType, navigation.ForeignKey.PrincipalEntityType) as JsonConverter;
            return converter!;
        }

        var complexProperties = dbEntityType.GetComplexProperties();
        var complexProperty = complexProperties.FirstOrDefault(x => x.ClrType == argType);
        if (complexProperty != null)
        {
            var converter = Activator.CreateInstance(conveterType, complexProperty) as JsonConverter;
            return converter!;
        }

        if (_navigations.Any())
        {
            foreach (var navigationEntity in _navigations)
            {
                var childNavigations = navigationEntity.GetNavigations();
                var childNavigation = childNavigations
                    .FirstOrDefault(x => x.ForeignKey.DeclaringEntityType.ClrType == argType);
                if (childNavigation != null)
                {
                    if (!_navigations.Contains(childNavigation.ForeignKey.DeclaringEntityType))
                        _navigations.Add(childNavigation.ForeignKey.DeclaringEntityType);

                    var converter = Activator.CreateInstance(conveterType, childNavigation.ForeignKey.DeclaringEntityType) as JsonConverter;
                    return converter!;
                }
            }
        }

        throw new NotImplementedException();
    }
}
