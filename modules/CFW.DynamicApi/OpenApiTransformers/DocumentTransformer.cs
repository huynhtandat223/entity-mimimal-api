using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.ApiExplorer;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi.Models;

namespace CFW.DynamicApi.OpenApiTransformers;

public class DocumentTransformer : IOpenApiDocumentTransformer
{
    private static OpenApiSchemaMapper _openApiSchemaMapper = new OpenApiSchemaMapper();

    public Task TransformAsync(OpenApiDocument document, OpenApiDocumentTransformerContext context, CancellationToken cancellationToken)
    {
        var newPaths = new OpenApiPaths();

        // Copy existing paths
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
                if (!newPaths.TryGetValue(operation.RelativePath, out var pathItem))
                {
                    pathItem = new OpenApiPathItem();
                    newPaths[operation.RelativePath] = pathItem;
                }

                if (Enum.TryParse<OperationType>(operation.HttpMethod, true, out var opType))
                {
                    var openApiOperation = new OpenApiOperation
                    {
                        // Basic metadata
                        OperationId = GenerateOperationId(operation),
                        Summary = operation.ActionDescriptor?.DisplayName ?? $"{operation.HttpMethod} {operation.RelativePath}",
                        Description = GetOperationDescription(operation),
                        Tags = new List<OpenApiTag> { new OpenApiTag { Name = group.GroupName } },

                        // Parameters (query, path, header, etc.)
                        Parameters = BuildParameters(operation),

                        // Request body for POST/PUT/PATCH operations
                        RequestBody = BuildRequestBody(operation),

                        // Response specifications
                        Responses = BuildResponses(operation),

                        // Security requirements
                        Security = BuildSecurityRequirements(operation),

                        // Additional properties
                        Deprecated = IsOperationDeprecated(operation)
                    };

                    pathItem.Operations[opType] = openApiOperation;
                }
            }
        }

        document.Paths = newPaths;

        PopulateDocumentSchemas(document);

        return Task.CompletedTask;
    }

    private void PopulateDocumentSchemas(OpenApiDocument document)
    {
        // Ensure Components section exists
        if (document.Components == null)
            document.Components = new OpenApiComponents();

        if (document.Components.Schemas == null)
            document.Components.Schemas = new Dictionary<string, OpenApiSchema>();

        // Add all component schemas from the mapper
        foreach (var schema in _openApiSchemaMapper.ComponentSchemas)
        {
            // Only add if not already present (avoid duplicates)
            if (!document.Components.Schemas.ContainsKey(schema.Key))
            {
                document.Components.Schemas[schema.Key] = schema.Value;
            }
        }
    }

    private string GenerateOperationId(ApiDescription operation)
    {
        var apiOperation = operation.ActionDescriptor.EndpointMetadata
            .OfType<DynamicApiOperation>()
            .FirstOrDefault();

        if (apiOperation is not null)
        {
            return apiOperation.Name ?? apiOperation.TargetType.Name;
        }

        throw new NotImplementedException();
    }

    private string GetOperationDescription(ApiDescription operation)
    {
        // Extract from XML documentation, attributes, or generate default
        return operation.ActionDescriptor?.DisplayName ??
               $"Performs {operation.HttpMethod} operation on {operation.RelativePath}";
    }

    private List<OpenApiParameter> BuildParameters(ApiDescription operation)
    {
        var parameters = new List<OpenApiParameter>();

        foreach (var paramDesc in operation.ParameterDescriptions)
        {
            // Skip body parameters - they're handled in RequestBody
            if (paramDesc.Source == BindingSource.Body)
                continue;

            parameters.Add(new OpenApiParameter
            {
                Name = paramDesc.Name,
                In = MapParameterLocation(paramDesc.Source),
                Required = paramDesc.IsRequired,
                Schema = CreateSchemaForType(paramDesc.Type),
                Description = paramDesc.ModelMetadata?.Description ?? $"Parameter {paramDesc.Name}"
            });
        }

        return parameters;
    }

    private ParameterLocation MapParameterLocation(BindingSource source)
    {
        return source?.Id switch
        {
            "Query" => ParameterLocation.Query,
            "Path" => ParameterLocation.Path,
            "Header" => ParameterLocation.Header,
            _ => ParameterLocation.Query
        };
    }

    private OpenApiRequestBody BuildRequestBody(ApiDescription operation)
    {
        var bodyParam = operation.ParameterDescriptions
            .FirstOrDefault(p => p.Source == BindingSource.Body);

        if (bodyParam?.Type is not null)
        {
            return _openApiSchemaMapper.CreateRequestBody(bodyParam.Type, true);
        }

        var apiOperation = operation.ActionDescriptor.EndpointMetadata
            .OfType<DynamicApiOperation>()
            .FirstOrDefault();

        if (apiOperation is not null && apiOperation.TargetType is not null)
        {
            return _openApiSchemaMapper.CreateRequestBody(apiOperation.TargetType, true);
        }

        throw new NotImplementedException();
    }

    private OpenApiResponses BuildResponses(ApiDescription operation)
    {
        var responses = new OpenApiResponses();

        // Add responses based on supported response types
        foreach (var responseType in operation.SupportedResponseTypes)
        {
            var statusCode = responseType.StatusCode.ToString();
            responses[statusCode] = new OpenApiResponse
            {
                Description = GetResponseDescription(responseType.StatusCode),
                Content = responseType.Type != typeof(void)
                    ? new Dictionary<string, OpenApiMediaType>
                    {
                        ["application/json"] = new OpenApiMediaType
                        {
                            Schema = CreateSchemaForType(responseType.Type)
                        }
                    }
                    : new Dictionary<string, OpenApiMediaType>()
            };
        }

        // Always add a default error response if none specified
        if (!responses.Any())
        {
            responses["200"] = new OpenApiResponse
            {
                Description = "Success",
                Content = new Dictionary<string, OpenApiMediaType>()
            };
        }

        return responses;
    }

    private string GetResponseDescription(int statusCode)
    {
        return statusCode switch
        {
            200 => "Success",
            201 => "Created",
            204 => "No Content",
            400 => "Bad Request",
            401 => "Unauthorized",
            403 => "Forbidden",
            404 => "Not Found",
            500 => "Internal Server Error",
            _ => "Response"
        };
    }

    private List<OpenApiSecurityRequirement> BuildSecurityRequirements(ApiDescription operation)
    {
        // Check if the operation requires authentication
        var requiresAuth = operation.ActionDescriptor
            .EndpointMetadata
            .Any(m => m is AuthorizeAttribute);

        if (requiresAuth)
        {
            return new List<OpenApiSecurityRequirement>
        {
            new OpenApiSecurityRequirement
            {
                {
                    new OpenApiSecurityScheme
                    {
                        Reference = new OpenApiReference
                        {
                            Type = ReferenceType.SecurityScheme,
                            Id = "Bearer" // Must match security scheme defined in document
                        }
                    },
                    new List<string>()
                }
            }
        };
        }

        return new List<OpenApiSecurityRequirement>();
    }

    private bool IsOperationDeprecated(ApiDescription operation)
    {
        return operation.ActionDescriptor
            .EndpointMetadata
            .Any(m => m is ObsoleteAttribute);
    }

    private OpenApiSchema CreateSchemaForType(Type type)
    {
        // This is a simplified schema creation - you might want to use
        // a more sophisticated approach or leverage existing schema generators
        if (type == typeof(string))
            return new OpenApiSchema { Type = "string" };
        if (type == typeof(int) || type == typeof(int?))
            return new OpenApiSchema { Type = "integer", Format = "int32" };
        if (type == typeof(bool) || type == typeof(bool?))
            return new OpenApiSchema { Type = "boolean" };
        if (type == typeof(DateTime) || type == typeof(DateTime?))
            return new OpenApiSchema { Type = "string", Format = "date-time" };

        // For complex types, you'd typically reference a schema component
        return new OpenApiSchema
        {
            Type = "object",
            Reference = new OpenApiReference
            {
                Type = ReferenceType.Schema,
                Id = type.Name
            }
        };
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
