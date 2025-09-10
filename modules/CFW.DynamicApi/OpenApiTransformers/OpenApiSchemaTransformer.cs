using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi.Models;

namespace CFW.DynamicApi.OpenApiTransformers;
public class OpenApiSchemaTransformer : IOpenApiSchemaTransformer
{
    public Task TransformAsync(OpenApiSchema schema, OpenApiSchemaTransformerContext context, CancellationToken cancellationToken)
    {
        //schema.Annotations?.Clear();
        //schema.Properties.Clear();
        //schema.Required.Clear();
        //schema.Extensions.Clear();
        //schema.Type = null;
        //schema.Format = null;
        //schema.Description = null;

        return Task.CompletedTask;
    }
}
