using CFW.DynamicApi;
using Microsoft.AspNetCore.Http.Json;
using Microsoft.AspNetCore.OData.Query;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.Extensions.Options;
using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;
using System.Text.Json;

namespace CFW.AppHost.Features.Endpoints.Services;

public class RuntimeOpenApiQueryOperationTransformer : IOpenApiOperationTransformer
{
    static readonly Dictionary<Type, (string type, string? format)> _clrToOpenApiMap = new()
    {
        [typeof(string)] = ("string", null),
        [typeof(int)] = ("integer", "int32"),
        [typeof(long)] = ("integer", "int64"),
        [typeof(bool)] = ("boolean", null),
        [typeof(DateTime)] = ("string", "date-time"),
        [typeof(decimal)] = ("number", "double"),
        [typeof(float)] = ("number", "float"),
        [typeof(double)] = ("number", "double"),
        [typeof(Guid)] = ("string", "uuid"),
        [typeof(byte)] = ("integer", "int32"),
        [typeof(byte[])] = ("string", "byte")
    };

    public Task TransformAsync(OpenApiOperation operation, OpenApiOperationTransformerContext context, CancellationToken cancellationToken)
    {
        var apiOperation = context.Description.ActionDescriptor.EndpointMetadata
            .OfType<Models.Endpoint>()
            .FirstOrDefault();

        if (apiOperation is null)
            return Task.CompletedTask;

        if (apiOperation.RuntimeEntityDefinition is null)
            return Task.CompletedTask;

        operation.Parameters ??= new List<OpenApiParameter>();

        //if (apiOperation.Route.IsNotNullOrNotWhiteSpace() && apiOperation.Route.Contains("{id}"))
        //{
        //    operation.Parameters.Add(new OpenApiParameter
        //    {
        //        Name = "id",
        //        In = ParameterLocation.Path,
        //        Required = true,
        //        Schema = new OpenApiSchema
        //        {
        //            Type = "string"
        //        }
        //    });
        //}

        var jsonOptions = context.ApplicationServices.GetService<IOptions<JsonOptions>>();
        var propertyNamingPolicy = jsonOptions!.Value.SerializerOptions.PropertyNamingPolicy;

        //var properties = apiOperation.AllowedProperties;
        var properties = apiOperation.RuntimeEntityDefinition!.Properties.Select(x => new PropertyMetadata
        {
            ClrType = Type.GetType(x.Type)!,
            IsKey = x.IsKey,
            IsRequired = x.IsRequired,
            Name = x.Name,
            PropertyType = PropertyType.Scalar
        });

        if (properties is null || !properties.Any())
            return Task.CompletedTask;

        var (entityProperties, requiredProperties) = BuildProperties(properties, propertyNamingPolicy!);

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
                ["@odata.count"] = new OpenApiSchema { Type = "integer", Format = "int64", Nullable = true }
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
                    ["application/json"] = new OpenApiMediaType { Schema = responseSchema }
                }
            };
        }
        else if (context.Description.HttpMethod == HttpMethods.Post
            || context.Description.HttpMethod == HttpMethods.Put
            || context.Description.HttpMethod == HttpMethods.Patch)
        {
            operation.RequestBody = new OpenApiRequestBody
            {
                Required = true,
                Content = new Dictionary<string, OpenApiMediaType>
                {
                    ["application/json"] = new OpenApiMediaType { Schema = entitySchema }
                }
            };

            operation.Responses["200"] = new OpenApiResponse
            {
                Description = "Successful mutation response",
                Content = new Dictionary<string, OpenApiMediaType>
                {
                    ["application/json"] = new OpenApiMediaType { Schema = responseSchema }
                }
            };
        }
        else if (context.Description.HttpMethod == HttpMethods.Delete)
        {
            operation.Responses["204"] = new OpenApiResponse
            {
                Description = "Successful deletion response"
            };
        }

        return Task.CompletedTask;
    }

    private static (Dictionary<string, OpenApiSchema> properties, HashSet<string> required) BuildProperties(IEnumerable<PropertyMetadata> props, JsonNamingPolicy namingPolicy)
    {
        var properties = new Dictionary<string, OpenApiSchema>();
        var required = new HashSet<string>();

        foreach (var prop in props)
        {
            var name = namingPolicy.ConvertName(prop.Name);
            var schema = BuildSchema(prop, namingPolicy);

            properties[name] = schema;
            if (prop.IsRequired)
                required.Add(name);
            else
                schema.Nullable = true;
        }

        return (properties, required);
    }

    private static OpenApiSchema BuildSchema(PropertyMetadata prop, JsonNamingPolicy namingPolicy)
    {
        Type GetUnderlyingType(Type type) => Nullable.GetUnderlyingType(type) ?? type;
        var actualType = GetUnderlyingType(prop.ClrType);

        var schema = new OpenApiSchema();

        switch (prop.PropertyType)
        {
            case PropertyType.Scalar:
                if (actualType.IsEnum)
                {
                    var enumValues = Enum.GetValues(actualType).Cast<object>();
                    var enumUnderlyingType = Enum.GetUnderlyingType(actualType);

                    if (enumUnderlyingType == typeof(int))
                    {
                        schema.Type = "integer";
                        schema.Enum = enumValues
                            .Select(val => (IOpenApiAny)new Microsoft.OpenApi.Any.OpenApiInteger(Convert.ToInt32(val)))
                            .ToList();
                    }
                    else
                    {
                        schema.Type = "string";
                        schema.Enum = enumValues
                            .Select(val => (IOpenApiAny)new Microsoft.OpenApi.Any.OpenApiString(val.ToString()))
                            .ToList();
                    }
                }
                else if (_clrToOpenApiMap.TryGetValue(actualType, out var scalar))
                {
                    schema.Type = scalar.type;
                    schema.Format = scalar.format;
                }
                else
                {
                    schema.Type = "object"; // Fallback
                }
                break;

            case PropertyType.Complex:
                schema.Type = "object";
                schema.Properties = new Dictionary<string, OpenApiSchema>();
                schema.Required = new HashSet<string>();

                if (prop.ChildProperties is not null)
                {
                    foreach (var child in prop.ChildProperties)
                    {
                        var childSchema = BuildSchema(child, namingPolicy);
                        var childName = namingPolicy.ConvertName(child.Name);
                        schema.Properties[childName] = childSchema;

                        if (child.IsRequired)
                            schema.Required.Add(childName);
                        else
                            childSchema.Nullable = true;
                    }
                }
                break;

            case PropertyType.Collection:
                schema.Type = "array";

                if (prop.ChildProperties is not null && prop.ChildProperties.Any())
                {
                    var itemSchema = new OpenApiSchema
                    {
                        Type = "object",
                        Properties = new Dictionary<string, OpenApiSchema>(),
                        Required = new HashSet<string>()
                    };

                    foreach (var child in prop.ChildProperties)
                    {
                        var childSchema = BuildSchema(child, namingPolicy);
                        var childName = namingPolicy.ConvertName(child.Name);
                        itemSchema.Properties[childName] = childSchema;

                        if (child.IsRequired)
                            itemSchema.Required.Add(childName);
                        else
                            childSchema.Nullable = true;
                    }

                    schema.Items = itemSchema;
                }
                else if (_clrToOpenApiMap.TryGetValue(actualType, out var itemType))
                {
                    schema.Items = new OpenApiSchema { Type = itemType.type, Format = itemType.format };
                }
                else
                {
                    schema.Items = new OpenApiSchema { Type = "object" };
                }
                break;
        }

        return schema;
    }

    static void AddOpenApiParameter(OpenApiOperation operation, AllowedQueryOptions options)
    {
        var map = new Dictionary<string, Func<OpenApiParameter>>
        {
            { "$filter", () => new OpenApiParameter { Name = "$filter", In = ParameterLocation.Query, Description = "Filter the results using OData syntax.", Required = false, Schema = new OpenApiSchema { Type = "string" } } },
            { "$top", () => new OpenApiParameter { Name = "$top", In = ParameterLocation.Query, Description = "Specify the number of records to return.", Required = false, Schema = new OpenApiSchema { Type = "integer", Format = "int32" } } },
            { "$skip", () => new OpenApiParameter { Name = "$skip", In = ParameterLocation.Query, Description = "Specify the number of records to skip.", Required = false, Schema = new OpenApiSchema { Type = "integer", Format = "int32" } } },
            { "$orderby", () => new OpenApiParameter { Name = "$orderby", In = ParameterLocation.Query, Description = "Specify the order of results.", Required = false, Schema = new OpenApiSchema { Type = "string" } } },
            { "$expand", () => new OpenApiParameter { Name = "$expand", In = ParameterLocation.Query, Description = "Expand related entities.", Required = false, Schema = new OpenApiSchema { Type = "string" } } },
            { "$select", () => new OpenApiParameter { Name = "$select", In = ParameterLocation.Query, Description = "Specify the fields to return.", Required = false, Schema = new OpenApiSchema { Type = "string" } } },
            { "$count", () => new OpenApiParameter { Name = "$count", In = ParameterLocation.Query, Description = "Include the count of the total matching entities.", Required = false, Schema = new OpenApiSchema { Type = "boolean" } } }
        };

        foreach (var kv in map)
        {
            var optionEnum = (AllowedQueryOptions)Enum.Parse(typeof(AllowedQueryOptions), kv.Key.TrimStart('$'), true);
            if (options.HasFlag(optionEnum))
                operation.Parameters.Add(kv.Value());
        }
    }
}

