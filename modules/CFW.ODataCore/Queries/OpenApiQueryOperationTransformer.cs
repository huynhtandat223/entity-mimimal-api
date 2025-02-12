using CFW.EntityApi.Registrators;
using Microsoft.AspNetCore.OData.Query;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi.Models;

namespace CFW.EntityApi.Queries;

public class OpenApiQueryOperationTransformer : IOpenApiOperationTransformer
{
    public Task TransformAsync(OpenApiOperation operation, OpenApiOperationTransformerContext context, CancellationToken cancellationToken)
    {
        var containerMemberRegistrationContext = context.Description.ActionDescriptor.EndpointMetadata
            .OfType<ContainerMemberRegistrationContext>()
            .FirstOrDefault();

        if (containerMemberRegistrationContext is null)
            return Task.CompletedTask;

        operation.Parameters ??= new List<OpenApiParameter>();
        AddOpenApiParameter(operation, AllowedQueryOptions.All);

        var memberRouter = containerMemberRegistrationContext.MemberRouter;
        if (memberRouter is not IDbEntityApiQueryRouter dbEntityApiQueryRouter)
            return Task.CompletedTask;

        var entityType = dbEntityApiQueryRouter.EntityType;
        var entityProperties = new Dictionary<string, OpenApiSchema>();
        foreach (var property in entityType.GetProperties())
        {
            var propertySchema = new OpenApiSchema();

            // Determine the property type and set OpenAPI schema type
            if (property.ClrType == typeof(string))
            {
                propertySchema.Type = "string";
            }
            else if (property.ClrType == typeof(int))
            {
                propertySchema.Type = "integer";
                propertySchema.Format = "int32";
            }
            else if (property.ClrType == typeof(long))
            {
                propertySchema.Type = "integer";
                propertySchema.Format = "int64";
            }
            else if (property.ClrType == typeof(bool))
            {
                propertySchema.Type = "boolean";
            }
            else if (property.ClrType == typeof(DateTime))
            {
                propertySchema.Type = "string";
                propertySchema.Format = "date-time";
            }
            else if (property.ClrType == typeof(decimal) || property.ClrType == typeof(float) || property.ClrType == typeof(double))
            {
                propertySchema.Type = "number";
                propertySchema.Format = "double";
            }
            else
            {
                propertySchema.Type = "object"; // Default to object if type is unknown
            }

            // Add the property to the schema
            entityProperties[property.Name] = propertySchema;
        }

        // Define the OData query result schema
        var schema = new OpenApiSchema
        {
            Type = "object",
            Properties = new Dictionary<string, OpenApiSchema>
            {
                ["value"] = new OpenApiSchema
                {
                    Type = "object",
                    Properties = entityProperties
                },
                ["totalCount"] = new OpenApiSchema
                {
                    Type = "integer",
                    Format = "int64",
                    Nullable = true
                }
            }
        };

        // Assign schema to response
        operation.Responses["200"] = new OpenApiResponse
        {
            Description = "Successful response with OData query result",
            Content = new Dictionary<string, OpenApiMediaType>
            {
                ["application/json"] = new OpenApiMediaType
                {
                    Schema = schema
                }
            }
        };

        return Task.CompletedTask;
    }

    static void AddOpenApiParameter(OpenApiOperation g, AllowedQueryOptions allowedQueryOptions)
    {
        var queryOptionMap = new Dictionary<string, Func<OpenApiParameter>>
        {
            { "$filter", () => new OpenApiParameter
                {
                    Name = "$filter",
                    In = ParameterLocation.Query,
                    Description = "Filter the results using OData syntax.",
                    Required = false,
                    Schema = new OpenApiSchema { Type = "string" }
                }
            },
            { "$top", () => new OpenApiParameter
                {
                    Name = "$top",
                    In = ParameterLocation.Query,
                    Description = "Specify the number of records to return.",
                    Required = false,
                    Schema = new OpenApiSchema { Type = "integer", Format = "int32" }
                }
            },
            { "$skip", () => new OpenApiParameter
                {
                    Name = "$skip",
                    In = ParameterLocation.Query,
                    Description = "Specify the number of records to skip.",
                    Required = false,
                    Schema = new OpenApiSchema { Type = "integer", Format = "int32" }
                }
            },
            { "$orderby", () => new OpenApiParameter
                {
                    Name = "$orderby",
                    In = ParameterLocation.Query,
                    Description = "Specify the order of results.",
                    Required = false,
                    Schema = new OpenApiSchema { Type = "string" }
                }
            },
            { "$expand", () => new OpenApiParameter
                {
                    Name = "$expand",
                    In = ParameterLocation.Query,
                    Description = "Expand related entities.",
                    Required = false,
                    Schema = new OpenApiSchema { Type = "string" }
                }
            },
            { "$select", () => new OpenApiParameter
                {
                    Name = "$select",
                    In = ParameterLocation.Query,
                    Description = "Specify the fields to return.",
                    Required = false,
                    Schema = new OpenApiSchema { Type = "string" }
                }
            },
            { "$count", () => new OpenApiParameter
                {
                    Name = "$count",
                    In = ParameterLocation.Query,
                    Description = "Include the count of the total matching entities.",
                    Required = false,
                    Schema = new OpenApiSchema { Type = "boolean" }
                }
            }
        };

        foreach (var queryOption in queryOptionMap)
        {
            var optionEnum = (AllowedQueryOptions)Enum.Parse(typeof(AllowedQueryOptions), queryOption.Key.TrimStart('$'), true);
            if (allowedQueryOptions.HasFlag(optionEnum))
            {
                g.Parameters.Add(queryOption.Value());
            }
        }

        g.Responses["200"].Content.Add("application/json", new OpenApiMediaType
        {
            Schema = new OpenApiSchema
            {
                Type = "object",
                Items = new OpenApiSchema
                {
                    Type = "array",
                    Items = new OpenApiSchema
                    {
                        Type = "object"
                    }
                }

            }
        });
    }
}
