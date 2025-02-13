using Microsoft.AspNetCore.OData.Query;

namespace CFW.EntityApi.Queries;

public interface IEntityApiQuery<TEntity>
{
    Task<IQueryable> ExecuteQuery(ODataQueryOptions<TEntity> queryOptions, CancellationToken cancellation = default);
}

public class DefaultEntityApiQuery<TEntity> : IEntityApiQuery<TEntity>
{
    private readonly IQueryable<TEntity> _queryable;
    public DefaultEntityApiQuery(IQueryable<TEntity> queryable)
    {
        _queryable = queryable;
    }

    public async Task<IQueryable> ExecuteQuery(ODataQueryOptions<TEntity> queryOptions, CancellationToken _)
    {
        var result = queryOptions.ApplyTo(_queryable);
        return await Task.FromResult(result);
    }
}
