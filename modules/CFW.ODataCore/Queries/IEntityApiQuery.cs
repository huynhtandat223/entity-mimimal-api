namespace CFW.EntityApi.Queries;

public interface IEntityApiQuery<TEntity>
{
    IQueryable<TEntity> GetQueryable();
}
