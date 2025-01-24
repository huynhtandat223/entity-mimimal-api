using CFW.ODataCore.Intefaces;
using CFW.ODataCore.Models;
using CFW.ODataCore.Models.Metadata;
using Microsoft.AspNetCore.OData;
using Microsoft.AspNetCore.OData.Query;
using Microsoft.OpenApi.Models;

namespace CFW.ODataCore.RouteMappers;

public class ODataQueryResult<T>
{
    public IEnumerable<T> Value { get; set; } = Array.Empty<T>();

    public long TotalCount { get; set; }
}

public static class RouteMapperExtensions
{
    /// <summary>
    /// https://learn.microsoft.com/en-us/aspnet/core/fundamentals/routing?view=aspnetcore-9.0
    /// </summary>
    private static readonly Dictionary<Type, string> _typeToConstraintMap = new()
    {
        { typeof(int), "int" },
        { typeof(bool), "bool" },
        { typeof(DateTime), "datetime" },
        { typeof(decimal), "decimal" },
        { typeof(double), "double" },
        { typeof(float), "float" },
        { typeof(Guid), "guid" },
        { typeof(long), "long" },
        { typeof(string), "alpha" } // Example: alpha for alphabetic strings
    };

    public static string GetKeyPattern<TKey>(this IRouteMapper routeMapper)
    {
        return _typeToConstraintMap.TryGetValue(typeof(TKey), out var constraint)
            ? $"{{key:{constraint}}}"
            : "{key}";
    }

    public static RouteHandlerBuilder AddODataMetadata<TEntity>(this RouteHandlerBuilder routeHandlerBuilder
        , MetadataEntity metadata, ODataOptions oDataOptions, EntityEndpoint<TEntity> entityEndpoint)
        where TEntity : class
    {
        return routeHandlerBuilder
        .WithOpenApi(g =>
        {
            // Mapping of allowed OData query options to OpenAPI parameters
            var allowedQueryOptions = entityEndpoint.AllowedQueryOptions ?? metadata
            .ODataQueryOptions.AllowedQueryOptions ?? AllowedQueryOptions.All; // Nullable

            AddOpenApiParameter(g, allowedQueryOptions);

            return g;
        })
        .Produces<ODataQueryResult<TEntity>>();

    }


    private static void AddOpenApiParameter(OpenApiOperation g, AllowedQueryOptions allowedQueryOptions)
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

    }
}
