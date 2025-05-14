using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using System.Text.Json;
using System.Web;

namespace CFW.DynamicApi;

public class QueryRequest<TRequest>
{
    public TRequest? QueryModel { get; set; }

    public static async ValueTask<QueryRequest<TRequest>> BindAsync(HttpContext context)
    {
        var request = context.Request;
        var jsonOptions = context.RequestServices.GetRequiredService<IOptions<JsonOptions>>().Value;
        var result = new QueryRequest<TRequest>();

        // Merge: query + route + body
        var allValues = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);

        // Query string
        if (request.QueryString.HasValue)
        {
            var queryDictionary = HttpUtility.ParseQueryString(request.QueryString.Value);
            foreach (var key in queryDictionary.AllKeys)
            {
                if (key != null)
                    allValues[key] = queryDictionary[key];
            }
        }

        // Route values
        foreach (var routeValue in request.RouteValues)
        {
            if (routeValue.Value != null)
                allValues[routeValue.Key] = routeValue.Value;
        }

        // JSON body
        if (request.ContentLength > 0 &&
            request.ContentType?.Contains("application/json", StringComparison.OrdinalIgnoreCase) == true)
        {
            request.EnableBuffering();
            using var reader = new StreamReader(request.Body);
            var bodyText = await reader.ReadToEndAsync();
            if (!string.IsNullOrWhiteSpace(bodyText))
            {
                try
                {
                    var bodyObject = JsonSerializer.Deserialize<Dictionary<string, object>>(bodyText, jsonOptions.JsonSerializerOptions);
                    if (bodyObject != null)
                    {
                        foreach (var kvp in bodyObject)
                        {
                            allValues[kvp.Key] = kvp.Value;
                        }
                    }
                }
                catch (JsonException)
                {
                    // Optionally log or throw validation error
                }
            }

            // Reset body for further reading (if needed downstream)
            request.Body.Position = 0;
        }

        // Serialize merged dictionary to JSON and map to TRequest
        var json = JsonSerializer.Serialize(allValues, jsonOptions.JsonSerializerOptions);
        var model = JsonSerializer.Deserialize<TRequest>(json, jsonOptions.JsonSerializerOptions);

        result.QueryModel = model;
        return result;
    }
}
