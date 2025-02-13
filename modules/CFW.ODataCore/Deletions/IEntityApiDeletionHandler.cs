using Microsoft.EntityFrameworkCore;

namespace CFW.EntityApi.Deletions;

public interface IEntityApiDeletionHandler<TEntity, TKey>
{
    Task<Result> Handle(TKey key, CancellationToken cancellationToken);
}

public class DefaultEntityApiDeletion<TDbContext, TEntity, TKey> : IEntityApiDeletionHandler<TEntity, TKey>
    where TDbContext : DbContext
    where TEntity : class
{
    private readonly TDbContext _dbContext;

    public DefaultEntityApiDeletion(TDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Result> Handle(TKey key, CancellationToken cancellationToken)
    {
        var entity = _dbContext.Set<TEntity>().Find(key);
        _dbContext.Set<TEntity>().Remove(entity!);

        await _dbContext.SaveChangesAsync();

        return this.Success();
    }
}