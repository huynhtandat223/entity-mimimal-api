﻿using Microsoft.EntityFrameworkCore;

namespace CFW.ODataCore.EFCore;

public interface IODataDbContextProvider
{
    DbContext GetContext();
}

public class ContextProvider<TDbContext> : IODataDbContextProvider
    where TDbContext : DbContext
{
    private readonly TDbContext _dbContext;
    public ContextProvider(TDbContext dbContext)
    {
        _dbContext = dbContext;
    }
    public DbContext GetContext()
    {
        return _dbContext;
    }
}
