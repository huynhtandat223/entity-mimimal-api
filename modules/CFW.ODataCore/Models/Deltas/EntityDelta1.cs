using CFW.ODataCore.Models.Metadata;
using Microsoft.AspNetCore.Http.Json;
using Microsoft.Extensions.Options;
using System.Text.Json;

namespace CFW.ODataCore.Models.Deltas;

public class EntityDelta<TEntity> : EntityDelta
    where TEntity : class, new()
{
    public TEntity? Instance { get; set; }

    public EntityDelta()
    {
        Instance = new TEntity();
    }

    public override object? GetInstance() => Instance;

    public static ValueTask<EntityDelta<TEntity>?> BindAsync(HttpContext context)
    {
        var jsonOptions = context.RequestServices.GetRequiredService<IOptions<JsonOptions>>().Value;
        var metadata = context.GetEndpoint()!.Metadata.GetMetadata<MetadataEntity>();

        var serviceProvider = context.RequestServices;
        if (metadata!.KeyProperty is null)
        {
            metadata.InitSourceMetadata(serviceProvider);
        }


        EntityEndpoint<TEntity>? entityEndpoint = null;
        if (metadata!.ConfigurationType is not null)
        {
            var customEntityEndpoint = ActivatorUtilities
                .CreateInstance(serviceProvider, metadata.ConfigurationType) as EntityEndpoint<TEntity>;
            entityEndpoint = customEntityEndpoint!;
        }
        else
        {
            entityEndpoint = new EntityEndpoint<TEntity>();
        }

        entityEndpoint!.SetMetadata(metadata);

        var conveterFactory = new DeltaConverterFactory<TEntity>(entityEndpoint!);
        var customizedOptions = new JsonSerializerOptions(jsonOptions.SerializerOptions);
        customizedOptions.Converters.Add(conveterFactory);

        var delta = JsonSerializer.DeserializeAsync<EntityDelta<TEntity>>(context.Request.Body
            , customizedOptions);

        return delta;
    }
}
