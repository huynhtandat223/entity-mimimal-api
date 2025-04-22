using CFW.EntityApi.Registrators;
using Microsoft.AspNetCore.Http.Json;
using Microsoft.AspNetCore.OData.Query;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.Extensions.Options;
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

        var jsonOptions = context.ApplicationServices.GetService<IOptions<JsonOptions>>();
        var propertyNamingPolicy = jsonOptions!.Value.SerializerOptions.PropertyNamingPolicy;

        var properties = containerMemberRegistrationContext.EntityConfiguration.Properties;
        if (properties is null || !properties.Any())
            return Task.CompletedTask;

        var entityProperties = new Dictionary<string, OpenApiSchema>();
        var requiredProperties = new HashSet<string>();

        foreach (var property in properties)
        {
            var propertySchema = new OpenApiSchema();

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
                propertySchema.Type = "object";
            }

            var propertyName = propertyNamingPolicy!.ConvertName(property.Name);
            entityProperties[propertyName] = propertySchema;

            if (property.IsRequired)
            {
                requiredProperties.Add(propertyName);
            }
            else
            {
                propertySchema.Nullable = true;
            }
        }

        var entitySchema = new OpenApiSchema
        {
            Type = "object",
            Properties = entityProperties,
            Required = requiredProperties
        };

        var responseSchema = new OpenApiSchema
        {
            Type = "object",
            Properties = new Dictionary<string, OpenApiSchema>
            {
                ["value"] = entitySchema,
                ["totalCount"] = new OpenApiSchema { Type = "integer", Format = "int64", Nullable = true }
            }
        };

        if (context.Description.HttpMethod == HttpMethods.Get)
        {
            AddOpenApiParameter(operation, AllowedQueryOptions.All);

            operation.Responses["200"] = new OpenApiResponse
            {
                Description = "Successful response with OData query result",
                Content = new Dictionary<string, OpenApiMediaType>
                {
                    ["application/json"] = new OpenApiMediaType
                    {
                        Schema = responseSchema
                    }
                }
            };
        }
        else if (context.Description.HttpMethod == HttpMethods.Post ||
                 context.Description.HttpMethod == HttpMethods.Put ||
                 context.Description.HttpMethod == HttpMethods.Patch ||
                 context.Description.HttpMethod == HttpMethods.Delete)
        {
            operation.RequestBody = new OpenApiRequestBody
            {
                Required = true,
                Content = new Dictionary<string, OpenApiMediaType>
                {
                    ["application/json"] = new OpenApiMediaType
                    {
                        Schema = entitySchema
                    }
                }
            };

            operation.Responses["200"] = new OpenApiResponse
            {
                Description = "Successful mutation response",
                Content = new Dictionary<string, OpenApiMediaType>
                {
                    ["application/json"] = new OpenApiMediaType
                    {
                        Schema = responseSchema
                    }
                }
            };
        }

        return Task.CompletedTask;
    }

    static void AddOpenApiParameter(OpenApiOperation g, AllowedQueryOptions allowedQueryOptions)
    {
        var queryOptionMap = new Dictionary<string, Func<OpenApiParameter>>
        {
            { "$filter", () => new OpenApiParameter { Name = "$filter", In = ParameterLocation.Query, Description = "Filter the results using OData syntax.", Required = false, Schema = new OpenApiSchema { Type = "string" } } },
            { "$top", () => new OpenApiParameter { Name = "$top", In = ParameterLocation.Query, Description = "Specify the number of records to return.", Required = false, Schema = new OpenApiSchema { Type = "integer", Format = "int32" } } },
            { "$skip", () => new OpenApiParameter { Name = "$skip", In = ParameterLocation.Query, Description = "Specify the number of records to skip.", Required = false, Schema = new OpenApiSchema { Type = "integer", Format = "int32" } } },
            { "$orderby", () => new OpenApiParameter { Name = "$orderby", In = ParameterLocation.Query, Description = "Specify the order of results.", Required = false, Schema = new OpenApiSchema { Type = "string" } } },
            { "$expand", () => new OpenApiParameter { Name = "$expand", In = ParameterLocation.Query, Description = "Expand related entities.", Required = false, Schema = new OpenApiSchema { Type = "string" } } },
            { "$select", () => new OpenApiParameter { Name = "$select", In = ParameterLocation.Query, Description = "Specify the fields to return.", Required = false, Schema = new OpenApiSchema { Type = "string" } } },
            { "$count", () => new OpenApiParameter { Name = "$count", In = ParameterLocation.Query, Description = "Include the count of the total matching entities.", Required = false, Schema = new OpenApiSchema { Type = "boolean" } } }
        };

        foreach (var queryOption in queryOptionMap)
        {
            var optionEnum = (AllowedQueryOptions)Enum.Parse(typeof(AllowedQueryOptions), queryOption.Key.TrimStart('$'), true);
            if (allowedQueryOptions.HasFlag(optionEnum))
            {
                g.Parameters.Add(queryOption.Value());
            }
        }
    }
}
