namespace CFW.DynamicApi.Entensions;

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi.Models;

public class ExampleDocumentTransformer : IOpenApiDocumentTransformer
{
    public class DynamicHandlerService
    {
        public Dictionary<string, Func<HttpContext, IResult>> PathHandlers { get; } = new()
        {
            ["users"] = ctx => Results.Ok(new { Message = "Users endpoint", Path = ctx.Request.Path }),
            ["orders"] = ctx => Results.Ok(new { Message = "Orders endpoint", Path = ctx.Request.Path }),
            ["products"] = ctx => Results.Ok(new { Message = "Products endpoint", Path = ctx.Request.Path }),
            // Add more static keys here. For runtime/dynamic keys, load from config/DB.
        };

        public IResult? Resolve(string path, HttpContext ctx)
        {
            if (PathHandlers.TryGetValue(path, out var handler))
            {
                return handler(ctx);
            }
            return Results.NotFound(new { Error = $"Path '{path}' not found in dictionary." });
        }
    }

    private DynamicHandlerService _handlerService = new DynamicHandlerService();
    // Inject the service to access the dictionary
    public ExampleDocumentTransformer()
    {
    }

    public Task TransformAsync(OpenApiDocument document, OpenApiDocumentTransformerContext context, CancellationToken cancellationToken)
    {
        // Create a new Paths collection to avoid modifying while iterating
        var newPaths = new OpenApiPaths();

        // Optionally preserve existing paths (e.g., /group/{path})
        foreach (var path in document.Paths)
        {
            newPaths[path.Key] = path.Value;
        }

        // Add specific operations for each dictionary key
        foreach (var pathKey in _handlerService.PathHandlers.Keys)
        {
            // Define the full path (e.g., /group/users)
            string fullPath = $"/group/{pathKey}";

            // Create a new PathItem for this path
            var pathItem = new OpenApiPathItem
            {
                Operations = new Dictionary<OperationType, OpenApiOperation>
                {
                    // Define supported methods (e.g., GET, POST, etc.)
                    [OperationType.Get] = CreateOperation(pathKey, "GET"),
                    [OperationType.Post] = CreateOperation(pathKey, "POST"),
                    // Add other methods as supported by MapMethods
                }
            };

            // Add to Paths (overwrite if it exists)
            newPaths[fullPath] = pathItem;
        }

        // Optionally remove the generic /group/{path} to avoid duplication
        // newPaths.Remove("/group/{path}");

        // Replace the document's Paths
        document.Paths = newPaths;

        return Task.CompletedTask;
    }

    private OpenApiOperation CreateOperation(string pathKey, string method)
    {
        return new OpenApiOperation
        {
            Summary = $"Handle {method} requests for {pathKey}",
            Description = $"Processes {method} requests for the '{pathKey}' sub-path under /group.",
            OperationId = $"Group_{pathKey}_{method}", // Unique operation ID
            Tags = new List<OpenApiTag> { new OpenApiTag { Name = "Dynamic Group" } },
            Responses = new OpenApiResponses
            {
                ["200"] = new OpenApiResponse
                {
                    Description = "Success",
                    Content = new Dictionary<string, OpenApiMediaType>
                    {
                        ["application/json"] = new OpenApiMediaType
                        {
                            Schema = new OpenApiSchema { Type = "object" } // Generic response
                        }
                    }
                },
                ["404"] = new OpenApiResponse
                {
                    Description = "Path not found in dictionary"
                }
            }
        };
    }
}
