using CFW.Core.Results;
using CFW.Core.Utils;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.AspNetCore.OData;
using Microsoft.AspNetCore.OData.Abstracts;
using Microsoft.AspNetCore.OData.Query;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.OData.ModelBuilder;
using Microsoft.OData.UriParser;
using System.Collections;
using IResult = CFW.Core.Results.IResult;

namespace CFW.DynamicApi.Interceptors.OData;

public class ODataFeatureInterceptor : IOperationInterceptor
{
    private readonly IServiceProvider _serviceProvider;
    public ODataFeatureInterceptor(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public async Task<object?> OnExecutedAsync(HttpContext httpContext, DynamicApiOperation operation, object? result)
    {
        var responseObj = result;
        if (responseObj is IResult r && responseObj.GetType().IsGenericType)
        {
            responseObj = r.GetPropertyValue(nameof(IResult<ODataFeatureInterceptor>.Data));
        }

        if (responseObj is null)
            return result;

        if (responseObj is not IEnumerable enumerable)
            return result;

        var responseObjType = responseObj.GetType();
        if (!responseObjType.IsGenericType)
            return result;

        var args = responseObjType.GetGenericArguments();
        if (args.Length != 1)
            return result;

        var entityType = responseObjType.GetGenericArguments()[0];
        var interceptorType = typeof(ODataFeatureInterceptor<>).MakeGenericType(entityType);

        var interceptor = (IOperationInterceptor)_serviceProvider.GetRequiredService(interceptorType);
        return await interceptor.OnExecutedAsync(httpContext, operation, responseObj);
    }
}

public class ODataFeatureInterceptor<TEntity> : IOperationInterceptor where TEntity : class
{
    private readonly ContainerConfiguration _containerConfiguration;
    public ODataFeatureInterceptor(ContainerConfiguration containerConfiguration)
    {
        _containerConfiguration = containerConfiguration;
    }

    public async Task<object?> OnExecutedAsync(HttpContext httpContext, DynamicApiOperation operation, object? result)
    {
        if (result is not IEnumerable enumerable)
            return result;

        var odataFeature = CreateODataFeature(httpContext.RequestServices);
        httpContext.Features.Set(odataFeature);

        var queryBuilder = new QueryBuilder(httpContext.Request.Query)
            .Where(q => !string.IsNullOrWhiteSpace(q.Value))
            .ToDictionary(q => q.Key, q => q.Value);

        var maxTop = _containerConfiguration.DefaultPageSize;
        var availableTop = new string[] { "$top", "top" };

        var topQuery = httpContext.Request.Query.SingleOrDefault(x => availableTop.Contains(x.Key.ToLower().Trim()));

        if (topQuery.Key.IsNullOrWhiteSpace())
            queryBuilder.Add("$top", maxTop.ToString());
        else
        {
            if (!int.TryParse(topQuery.Value, out var topValue))
                queryBuilder.Add("$top", maxTop.ToString());
            else if (topValue > maxTop)
            {
                queryBuilder.Add("$top", maxTop.ToString());
            }
        }

        var query = queryBuilder;

        // Step 1: Find $select key
        var selectKey = query.Keys
            .FirstOrDefault(k => string.Equals(k, "$select", StringComparison.OrdinalIgnoreCase) || string.Equals(k, "select", StringComparison.OrdinalIgnoreCase));

        // Step 2: Prepare allowed properties
        var allowedPropDict = operation.AllowedProperties
            .Where(x => x.PropertyType == PropertyType.Scalar)
            .ToDictionary(x => x.Name, x => x.Name, StringComparer.OrdinalIgnoreCase);
        var allowAllProps = operation.AllowAllProperties;

        List<string> selectedProps;

        if (!string.IsNullOrWhiteSpace(selectKey) && query.TryGetValue(selectKey, out var rawSelect))
        {
            var requestedProps = rawSelect.ToString()
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Select(p => p.Trim())
                .ToList();
            selectedProps = requestedProps;

            if (!allowAllProps)
            {
                selectedProps = requestedProps
                .Where(p => allowedPropDict.ContainsKey(p))
                .Select(p => allowedPropDict[p])
                .ToList();

                // fallback if nothing valid selected
                if (selectedProps.Count == 0)
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
        result = options.ApplyTo(queryable);

        return await Task.FromResult(new ODataResult(result));
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
