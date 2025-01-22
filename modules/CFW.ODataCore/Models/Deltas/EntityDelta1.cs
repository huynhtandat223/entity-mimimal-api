using CFW.ODataCore.Models.Metadata;
using Microsoft.AspNetCore.Http.Json;
using Microsoft.Extensions.Options;
using System.Text.Json;

namespace CFW.ODataCore.Models.Deltas;

public class EntityDelta<TEntity> : EntityDelta
    where TEntity : class
{
    public TEntity? Instance { get; set; } = Activator.CreateInstance<TEntity>();

    public EntityEndpoint<TEntity>? EntityConfiguration { get; set; }

    public override object? GetInstance() => Instance;

    public static async ValueTask<EntityDelta<TEntity>?> BindAsync(HttpContext context)
    {
        var jsonOptions = context.RequestServices.GetRequiredService<IOptions<JsonOptions>>().Value;
        var metadata = context.GetEndpoint()!.Metadata.GetMetadata<MetadataEntity>();

        if (metadata is null)
        {
            throw new InvalidOperationException("Metadata not found");
        }

        var serviceProvider = context.RequestServices;
        var entityConfig = metadata.GetOrCreateEndpointConfiguration<TEntity>(serviceProvider);

        var conveterFactory = new DeltaConverterFactory<TEntity>(entityConfig!);
        var customizedOptions = new JsonSerializerOptions(jsonOptions.SerializerOptions);
        customizedOptions.Converters.Add(conveterFactory);

        var delta = await JsonSerializer.DeserializeAsync<EntityDelta<TEntity>>(context.Request.Body
            , customizedOptions);

        if (delta is not null)
            delta.EntityConfiguration = entityConfig;

        return delta;
    }
}
