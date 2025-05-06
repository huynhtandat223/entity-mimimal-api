using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Json;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using System.Collections;
using System.Text.Json;

namespace CFW.DynamicApi.Deltas;

public class EntityDeltaSet
{
    public required Type ObjectType { get; set; }

    public List<EntityDelta> ChangedProperties { get; }
        = new List<EntityDelta>();

    public IList GetList()
    {
        var listType = typeof(List<>).MakeGenericType(ObjectType);
        var resultList = (IList)Activator.CreateInstance(listType)!;

        foreach (var item in ChangedProperties)
        {
            var instance = item.GetInstance();
            resultList.Add(instance);
        }
        return resultList;
    }
}

public class EntityDelta
{
    public IEntityType? EfCoreEntityType { get; set; }

    public IComplexProperty? EfCoreComplexProperty { get; set; }

    public Dictionary<string, object?> ChangedProperties { get; }
        = new Dictionary<string, object?>();

    public virtual object? GetInstance() { return default!; }
}

public class EntityDelta<TEntity> : EntityDelta
    where TEntity : class
{
    public TEntity? Instance { get; set; } = Activator.CreateInstance<TEntity>();

    public static async ValueTask<EntityDelta<TEntity>?> BindAsync(HttpContext context)
    {
        var apiOperation = context.GetEndpoint()!.Metadata.GetMetadata<DynamicApiOperation>()!;
        var jsonOptions = context.RequestServices.GetRequiredService<IOptions<JsonOptions>>().Value;

        var customizedOptions = new JsonSerializerOptions(jsonOptions.SerializerOptions);
        var factory = apiOperation.EntityGroup.CreateJsonConverterFactory(context.RequestServices);

        customizedOptions.Converters.Add(factory!);

        var delta = await JsonSerializer.DeserializeAsync<EntityDelta<TEntity>>(context.Request.Body
            , customizedOptions);

        return delta;
    }
}
