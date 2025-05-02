using CFW.Core.Utils;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.AspNetCore.Mvc.Formatters;
using Microsoft.AspNetCore.OData;
using Microsoft.AspNetCore.OData.Abstracts;
using Microsoft.AspNetCore.OData.Formatter;
using Microsoft.AspNetCore.OData.Query;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.OData.ModelBuilder;
using Microsoft.OData.UriParser;
using System.Collections;

namespace CFW.DynamicApi.Interceptors;

public class ODataFeatureInterceptor<TEntity> : IOperationInterceptor where TEntity : class
{
    private readonly ContainerConfiguration _containerConfiguration;
    public ODataFeatureInterceptor(ContainerConfiguration containerConfiguration)
    {
        _containerConfiguration = containerConfiguration;
    }

    public async Task OnExecutedAsync(HttpContext httpContext, DynamicApiOperation operation, object? result)
    {
        if (result is not IEnumerable enumerable)
            return;

        var formatter = httpContext.RequestServices.GetRequiredService<ODataOutputFormatter>();

        var odataFeature = CreateODataFeature(httpContext.RequestServices);
        httpContext.Features.Set(odataFeature);

        var queryBuilder = new QueryBuilder(httpContext.Request.Query);
        var maxTop = _containerConfiguration.DefaultPageSize;
        var availableTop = new string[] { "$top", "top" };

        var topQuery = httpContext.Request.Query.SingleOrDefault(x => availableTop.Contains(x.Key.ToLower().Trim()));

        if (topQuery.Key.IsNullOrWhiteSpace())
        {
            queryBuilder.Add("$top", maxTop.ToString());
        }
        else
        {
            if (!int.TryParse(topQuery.Value, out var topValue))
            {
                queryBuilder.Add("$top", maxTop.ToString());
            }
            else if (topValue > maxTop)
            {
                queryBuilder.Add("$top", maxTop.ToString());
            }
        }

        var query = Microsoft.AspNetCore.WebUtilities.QueryHelpers.ParseQuery(httpContext.Request.QueryString.Value ?? string.Empty);

        // Step 1: Find $select key
        var selectKey = query.Keys
            .FirstOrDefault(k => string.Equals(k, "$select", StringComparison.OrdinalIgnoreCase) || string.Equals(k, "select", StringComparison.OrdinalIgnoreCase));

        // Step 2: Prepare allowed properties
        var allowedPropDict = operation.AllowedProperties
            .Where(x => x.PropertyType == PropertyType.Scalar)
            .ToDictionary(x => x.Name, x => x.Name, StringComparer.OrdinalIgnoreCase);

        List<string> selectedProps;

        if (!string.IsNullOrWhiteSpace(selectKey) && query.TryGetValue(selectKey, out var rawSelect))
        {
            var requestedProps = rawSelect.ToString()
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Select(p => p.Trim())
                .ToList();

            selectedProps = requestedProps
                .Where(p => allowedPropDict.ContainsKey(p))
                .Select(p => allowedPropDict[p])
                .ToList();

            // fallback if nothing valid selected
            if (selectedProps.Count == 0)
            {
                selectedProps = allowedPropDict.Values.ToList();
            }
        }
        else
        {
            // No $select provided, use full list
            selectedProps = allowedPropDict.Values.ToList();
        }

        // Step 3: Overwrite the $select query param
        query["$select"] = string.Join(",", selectedProps);

        // Step 4: Rewrite the query string on the request
        var newQueryString = Microsoft.AspNetCore.WebUtilities.QueryHelpers.AddQueryString(string.Empty, query);
        httpContext.Request.QueryString = new QueryString(newQueryString);


        var odataQueryContext = new ODataQueryContext(odataFeature.Model, typeof(TEntity), odataFeature.Path);

        var options = new ODataQueryOptions<TEntity>(odataQueryContext, httpContext.Request);

        var queryable = enumerable.AsQueryable();
        options.ApplyTo(queryable);

        var formatterContext = new OutputFormatterWriteContext(httpContext,
            (stream, encoding) => new StreamWriter(stream, encoding),
            result.GetType() ?? typeof(object), result)
        {
            ContentType = "application/json;odata.metadata=none",
        };

        await formatter.WriteAsync(formatterContext);
    }

    public IODataFeature CreateODataFeature(IServiceProvider serviceProvider)
    {
        var entityName = typeof(TEntity).Name;
        var builder = new ODataConventionModelBuilder();
        var entitySet = builder.EntitySet<TEntity>(entityName);
        var routePrefix = StringUtils.SanitizeRoute(_containerConfiguration.RoutePrefix);

        var odataEntityType = builder.AddEntityType(typeof(TEntity));
        builder.AddEntitySet(entityName, odataEntityType);


        //TODO: handle this in a better way
        //odataEntityType.HasKey(keyPropertyInfo);
        builder.EnableLowerCamelCaseForPropertiesAndEnums();

        var model = builder.GetEdmModel();
        var edmEntitySet = model.EntityContainer.FindEntitySet(entityName);
        var entitySetSegment = new EntitySetSegment(edmEntitySet);
        var segments = new List<ODataPathSegment> { entitySetSegment };

        var path = new ODataPath(segments);
        var optionsFactory = serviceProvider.GetRequiredService<IOptionsFactory<ODataOptions>>();
        var odataOptions = optionsFactory.Create(routePrefix);

        if (odataOptions.QueryConfigurations.MaxTop is null
            || odataOptions.QueryConfigurations.MaxTop.Value < 1)
            odataOptions.QueryConfigurations.MaxTop = _containerConfiguration.DefaultPageSize;

        //ContainerRegistrationContext.ContainerConfiguration.ConfigureODataOptions?.Invoke(odataOptions);

        if (!odataOptions.RouteComponents.TryGetValue(routePrefix, out var routeComponent))
            odataOptions = odataOptions.AddRouteComponents(routePrefix, model);

        var oDataOptions = odataOptions;
        var odataFeature = new ODataFeature
        {
            Path = path,
            Model = model,
            RoutePrefix = routePrefix,
            Services = odataOptions.RouteComponents[routePrefix].ServiceProvider
        };

        return odataFeature;
    }
}
