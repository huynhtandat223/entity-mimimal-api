using Microsoft.OpenApi.Models;
using System.ComponentModel.DataAnnotations;
using System.Reflection;
using System.Text.Json.Serialization;

namespace CFW.DynamicApi.OpenApiTransformers;

public class OpenApiSchemaMapper
{
    private readonly Dictionary<Type, OpenApiSchema> _schemaCache = new();
    private readonly Dictionary<string, OpenApiSchema> _componentSchemas = new();
    private readonly HashSet<Type> _processingTypes = new(); // Prevent infinite recursion

    public Dictionary<string, OpenApiSchema> ComponentSchemas => _componentSchemas;

    public OpenApiSchema MapTypeToSchema(Type type)
    {
        if (_schemaCache.TryGetValue(type, out var cachedSchema))
            return cachedSchema;

        var schema = CreateSchemaForType(type);
        _schemaCache[type] = schema;
        return schema;
    }

    private OpenApiSchema CreateSchemaForType(Type type)
    {
        // Handle nullable types
        var underlyingType = Nullable.GetUnderlyingType(type);
        if (underlyingType != null)
        {
            var nullableSchema = CreateSchemaForType(underlyingType);
            nullableSchema.Nullable = true;
            return nullableSchema;
        }

        // Primitive types
        if (type == typeof(string))
            return new OpenApiSchema { Type = "string" };

        if (type == typeof(int) || type == typeof(uint))
            return new OpenApiSchema { Type = "integer", Format = "int32" };

        if (type == typeof(long) || type == typeof(ulong))
            return new OpenApiSchema { Type = "integer", Format = "int64" };

        if (type == typeof(float))
            return new OpenApiSchema { Type = "number", Format = "float" };

        if (type == typeof(double) || type == typeof(decimal))
            return new OpenApiSchema { Type = "number", Format = "double" };

        if (type == typeof(bool))
            return new OpenApiSchema { Type = "boolean" };

        if (type == typeof(DateTime) || type == typeof(DateTimeOffset))
            return new OpenApiSchema { Type = "string", Format = "date-time" };

        if (type == typeof(DateOnly))
            return new OpenApiSchema { Type = "string", Format = "date" };

        if (type == typeof(TimeOnly) || type == typeof(TimeSpan))
            return new OpenApiSchema { Type = "string", Format = "time" };

        if (type == typeof(Guid))
            return new OpenApiSchema { Type = "string", Format = "uuid" };

        // Enum types
        if (type.IsEnum)
        {
            return new OpenApiSchema
            {
                Type = "string",
                Enum = Enum.GetNames(type)
                    .Select(name => (Microsoft.OpenApi.Any.IOpenApiAny)new Microsoft.OpenApi.Any.OpenApiString(name))
                    .ToList()
            };
        }

        // Array/Collection types
        if (type.IsArray)
        {
            return new OpenApiSchema
            {
                Type = "array",
                Items = CreateSchemaForType(type.GetElementType()!)
            };
        }

        // Generic collections (List<T>, IEnumerable<T>, etc.)
        if (type.IsGenericType)
        {
            var genericDef = type.GetGenericTypeDefinition();
            if (genericDef == typeof(List<>) ||
                genericDef == typeof(IList<>) ||
                genericDef == typeof(ICollection<>) ||
                genericDef == typeof(IEnumerable<>))
            {
                var itemType = type.GetGenericArguments()[0];
                return new OpenApiSchema
                {
                    Type = "array",
                    Items = CreateSchemaForType(itemType)
                };
            }

            // Dictionary types
            if (genericDef == typeof(Dictionary<,>) || genericDef == typeof(IDictionary<,>))
            {
                var valueType = type.GetGenericArguments()[1];
                return new OpenApiSchema
                {
                    Type = "object",
                    AdditionalProperties = CreateSchemaForType(valueType)
                };
            }
        }

        // Complex object types
        return CreateObjectSchema(type);
    }

    private OpenApiSchema CreateObjectSchema(Type type)
    {
        var schemaId = GetSchemaId(type);

        // Create a reference to avoid circular dependencies
        var referenceSchema = new OpenApiSchema
        {
            Reference = new OpenApiReference
            {
                Type = ReferenceType.Schema,
                Id = schemaId
            }
        };

        // If we've already processed this type, return the reference
        if (_componentSchemas.ContainsKey(schemaId) || _processingTypes.Contains(type))
            return referenceSchema;

        // Mark as processing to prevent infinite recursion
        _processingTypes.Add(type);

        try
        {
            // Create the actual schema definition
            var schema = new OpenApiSchema
            {
                Type = "object",
                Properties = new Dictionary<string, OpenApiSchema>(),
                Required = new HashSet<string>()
            };

            // Add to component schemas first to handle circular references
            _componentSchemas[schemaId] = schema;

            // Get all public properties
            var properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Where(p => p.CanRead && p.GetIndexParameters().Length == 0); // Exclude indexer properties

            foreach (var property in properties)
            {
                // Skip properties marked with JsonIgnore
                if (property.GetCustomAttribute<JsonIgnoreAttribute>() != null)
                    continue;

                var propertyName = GetPropertyName(property);
                var propertySchema = CreateSchemaForType(property.PropertyType);

                schema.Properties[propertyName] = propertySchema;

                // Check if property is required
                if (IsPropertyRequired(property))
                {
                    schema.Required.Add(propertyName);
                }
            }

            return referenceSchema;
        }
        finally
        {
            // Remove from processing set
            _processingTypes.Remove(type);
        }
    }

    private string GetSchemaId(Type type)
    {
        if (type.IsGenericType)
        {
            var name = type.Name.Split('`')[0];
            var args = string.Join("", type.GetGenericArguments().Select(GetSchemaId));
            return $"{name}Of{args}";
        }
        return type.Name;
    }

    private string GetPropertyName(PropertyInfo property)
    {
        // Check for JsonPropertyName attribute
        var jsonPropertyAttr = property.GetCustomAttribute<JsonPropertyNameAttribute>();
        if (jsonPropertyAttr != null)
            return jsonPropertyAttr.Name;

        // Check for Newtonsoft JsonProperty attribute (safer approach)
        var newtonsoftAttrs = property.GetCustomAttributes()
            .Where(attr => attr.GetType().FullName == "Newtonsoft.Json.JsonPropertyAttribute");

        foreach (var attr in newtonsoftAttrs)
        {
            var propertyNameProp = attr.GetType().GetProperty("PropertyName");
            if (propertyNameProp?.GetValue(attr) is string propertyName && !string.IsNullOrEmpty(propertyName))
                return propertyName;
        }

        // Default to camelCase (but handle edge cases)
        if (string.IsNullOrEmpty(property.Name))
            return property.Name;

        if (property.Name.Length == 1)
            return char.ToLowerInvariant(property.Name[0]).ToString();

        return char.ToLowerInvariant(property.Name[0]) + property.Name[1..];
    }

    private bool IsPropertyRequired(PropertyInfo property)
    {
        // Check for Required attribute
        if (property.GetCustomAttribute<RequiredAttribute>() != null)
            return true;

        // Check for JsonRequired attribute
        if (property.GetCustomAttribute<JsonRequiredAttribute>() != null)
            return true;

        // Check if it's a non-nullable reference type (C# 8+ nullable context)
        try
        {
            var nullabilityContext = new NullabilityInfoContext();
            var nullabilityInfo = nullabilityContext.Create(property);

            // For reference types, check if it's non-nullable
            if (!property.PropertyType.IsValueType)
            {
                return nullabilityInfo.ReadState == NullabilityState.NotNull;
            }

            // For value types, only required if it's not nullable and has Required attribute
            return !IsNullableValueType(property.PropertyType) &&
                   property.GetCustomAttribute<RequiredAttribute>() != null;
        }
        catch
        {
            // If nullability check fails, fall back to simple logic
            return property.GetCustomAttribute<RequiredAttribute>() != null;
        }
    }

    private bool IsNullableValueType(Type type)
    {
        return Nullable.GetUnderlyingType(type) != null;
    }

    public OpenApiRequestBody CreateRequestBody(Type bodyType, bool isRequired = false, string? description = null)
    {
        return new OpenApiRequestBody
        {
            Description = description ?? "Request body",
            Content = new Dictionary<string, OpenApiMediaType>
            {
                ["application/json"] = new OpenApiMediaType
                {
                    Schema = MapTypeToSchema(bodyType)
                }
            },
            Required = isRequired
        };
    }

    /// <summary>
    /// Call this method to populate the OpenAPI document's component schemas
    /// </summary>
    public void PopulateComponentSchemas(OpenApiDocument document)
    {
        if (document.Components == null)
            document.Components = new OpenApiComponents();

        if (document.Components.Schemas == null)
            document.Components.Schemas = new Dictionary<string, OpenApiSchema>();

        foreach (var schema in _componentSchemas)
        {
            document.Components.Schemas[schema.Key] = schema.Value;
        }
    }

    /// <summary>
    /// Clear all cached schemas (useful for testing or when schema definitions change)
    /// </summary>
    public void ClearCache()
    {
        _schemaCache.Clear();
        _componentSchemas.Clear();
        _processingTypes.Clear();
    }
}