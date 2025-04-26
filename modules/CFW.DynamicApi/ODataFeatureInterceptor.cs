using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.OData.Query;
using Microsoft.OData.Edm;
using Microsoft.OData.UriParser;

namespace CFW.DynamicApi;

public class ODataFeatureInterceptor<TEntity> : IOperationInterceptor where TEntity : class
{
    private readonly EdmModel _edmModel;

    public ODataFeatureInterceptor()
    {
        _edmModel = new EdmModel();
    }

    public Task<object?> OnExecutingAsync(HttpContext context, DynamicApiOperation operation)
    {
        if (!context.Request.QueryString.HasValue)
            return Task.FromResult<object?>(null);

        var queryContext = new ODataQueryContext(_edmModel, typeof(TEntity), new ODataPath());
        var queryOptions = new ODataQueryOptions<TEntity>(queryContext, context.Request);
        context.Items["OData.QueryOptions"] = queryOptions;
        return Task.FromResult<object?>(null);
    }

    public Task<object?> OnExecutedAsync(HttpContext context, DynamicApiOperation operation, object? result)
    {
        if (result is IQueryable<TEntity> queryable &&
            context.Items.TryGetValue("OData.QueryOptions", out var optionsObj) &&
            optionsObj is ODataQueryOptions<TEntity> options)
        {
            var applied = options.ApplyTo(queryable, new ODataQuerySettings
            {
                HandleNullPropagation = HandleNullPropagationOption.True
            });

            return Task.FromResult<object?>(applied);
        }

        return Task.FromResult(result);
    }
}
