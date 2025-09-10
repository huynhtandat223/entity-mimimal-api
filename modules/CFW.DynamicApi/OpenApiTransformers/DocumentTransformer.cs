using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi.Models;

namespace CFW.DynamicApi.OpenApiTransformers;

public class DocumentTransformer : IOpenApiDocumentTransformer
{
    public Task TransformAsync(OpenApiDocument document, OpenApiDocumentTransformerContext context, CancellationToken cancellationToken)
    {
        // Create a new Paths collection to avoid modifying while iterating
        var newPaths = new OpenApiPaths();

        // Add non-group paths
        foreach (var path in document.Paths)
        {
            newPaths[path.Key] = path.Value;
        }

        foreach (var group in context.DescriptionGroups)
        {
            if (string.IsNullOrEmpty(group.GroupName))
                continue;

            foreach (var operation in group.Items)
            {
                // Ensure the path exists
                if (!newPaths.TryGetValue(operation.RelativePath, out var pathItem))
                {
                    pathItem = new OpenApiPathItem();
                    newPaths[operation.RelativePath] = pathItem;
                }

                // Map the HTTP method to OperationType
                if (Enum.TryParse<OperationType>(operation.HttpMethod, true, out var opType))
                {
                    // Build an OpenApiOperation from ApiDescription
                    var openApiOperation = new OpenApiOperation
                    {
                        Security = new List<OpenApiSecurityRequirement>
                        {
                            new OpenApiSecurityRequirement
                            {

                            }
                        },
                        Description = "test",
                        Tags = new List<OpenApiTag> { new OpenApiTag { Name = group.GroupName } },
                        OperationId = operation.ActionDescriptor?.DisplayName,
                        Summary = operation.HttpMethod + " " + operation.RelativePath
                        // TODO: parameters, responses, etc. depending on your generator
                    };

                    pathItem.Operations[opType] = openApiOperation;
                }
            }
        }

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
