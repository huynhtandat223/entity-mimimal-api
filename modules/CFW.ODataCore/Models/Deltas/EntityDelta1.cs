using CFW.EntityApi.Models.Builders;
using Microsoft.AspNetCore.Http.Json;
using Microsoft.Extensions.Options;
using System.Text.Json;

namespace CFW.EntityApi.Models.Deltas;

public class EntityDelta<TEntity> : EntityDelta
    where TEntity : class
{
    public TEntity? Instance { get; set; } = Activator.CreateInstance<TEntity>();

    public override object? GetInstance() => Instance;

    public static async ValueTask<EntityDelta<TEntity>?> BindAsync(HttpContext context)
    {
        var entityApiConfiguration = context.GetEndpoint()!.Metadata.GetMetadata<EntityApiConfiguration>()!;
        var jsonOptions = context.RequestServices.GetRequiredService<IOptions<JsonOptions>>().Value;

        var customizedOptions = new JsonSerializerOptions(jsonOptions.SerializerOptions);
        var factory = entityApiConfiguration.GetDeltaConverterFactory();

        customizedOptions.Converters.Add(factory!);

        var delta = await JsonSerializer.DeserializeAsync<EntityDelta<TEntity>>(context.Request.Body
            , customizedOptions);

        return delta;
    }


}
