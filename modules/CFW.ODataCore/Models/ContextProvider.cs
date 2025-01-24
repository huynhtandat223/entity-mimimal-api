using Microsoft.EntityFrameworkCore;

namespace CFW.ODataCore.Models;

[Obsolete("Use TDBContext directly")]
public interface IDbContextProvider
{
    DbContext GetDbContext();
}

[Obsolete("Use TDBContext directly")]
public class ContextProvider<TDbContext> : IDbContextProvider
    where TDbContext : DbContext
{
    private readonly TDbContext _dbContext;
    public ContextProvider(TDbContext dbContext)
    {
        _dbContext = dbContext;
    }
    public DbContext GetDbContext()
    {
        return _dbContext;
    }
}
