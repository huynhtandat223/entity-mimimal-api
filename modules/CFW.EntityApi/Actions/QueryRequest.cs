using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using System.Text.Json;
using System.Web;

namespace CFW.EntityApi.Actions;

public class QueryRequest<TRequest>
{
    public TRequest? QueryModel { get; set; }

    public static ValueTask<QueryRequest<TRequest>> BindAsync(HttpContext context)
    {
        var request = context.Request;
        var jsonOptions = context.RequestServices.GetRequiredService<IOptions<JsonOptions>>().Value;
        var result = new QueryRequest<TRequest>();

        // Create a dictionary to hold both query string and route values
        var allValues = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);

        // Add query string values, if any
        if (request.QueryString.HasValue)
        {
            var queryDictionary = HttpUtility.ParseQueryString(request.QueryString.Value);
            foreach (var key in queryDictionary.AllKeys)
            {
                if (key != null)
                    allValues[key] = queryDictionary[key];
            }
        }

        // Add route values; they are available in Request.RouteValues
        foreach (var routeValue in request.RouteValues)
        {
            if (routeValue.Value != null)
                allValues[routeValue.Key] = routeValue.Value.ToString();
        }

        // Serialize the merged dictionary to JSON and deserialize to your TRequest model
        string json = JsonSerializer.Serialize(allValues);
        var model = JsonSerializer.Deserialize<TRequest>(json, jsonOptions.JsonSerializerOptions);

        result.QueryModel = model;
        return new ValueTask<QueryRequest<TRequest>>(result);
    }
}


