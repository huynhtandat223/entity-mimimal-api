using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace CFW.DynamicApi.Deltas;

public class EntityDelta<TEntity> : EntityDelta
    where TEntity : class
{
    public TEntity? Instance { get; set; } = Activator.CreateInstance<TEntity>();

    public override object? GetInstance() => Instance;

    public static async ValueTask<EntityDelta<TEntity>?> BindAsync(HttpContext context)
    {
        var entity = context.GetEndpoint()!.Metadata.GetMetadata<DynamicApiOperation>()!;
        var jsonOptions = context.RequestServices.GetRequiredService<IOptions<JsonOptions>>().Value;

        throw new NotImplementedException();

        //var customizedOptions = new JsonSerializerOptions(jsonOptions.SerializerOptions);
        //var factory = entityApiConfiguration.GetDeltaConverterFactory();

        //if (factory is null && entityApiConfiguration.JsonConverterFactoryFunc is not null)
        //{
        //    factory = entityApiConfiguration.JsonConverterFactoryFunc(context.RequestServices);
        //}

        //customizedOptions.Converters.Add(factory!);

        //var delta = await JsonSerializer.DeserializeAsync<EntityDelta<TEntity>>(context.Request.Body
        //    , customizedOptions);

        //return delta;
    }


}
