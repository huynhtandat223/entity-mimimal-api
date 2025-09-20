using CFW.AppHost.Infrastructures.DbContextExtensions.Models;
using Microsoft.EntityFrameworkCore;

namespace CFW.AppHost.Infrastructures.DbContextExtensions.Services.Runtimes;

public class RuntimeDbContext : DbContext
{
    private readonly RuntimeTypeRegistry _runtimeTypeRegistry;

    public RuntimeDbContext(DbContextOptions<RuntimeDbContext> options, RuntimeTypeRegistry runtimeTypeRegistry) : base(options)
    {
        _runtimeTypeRegistry = runtimeTypeRegistry;
    }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        // Register all runtime types as entities in the DbContext
        foreach (var type in _runtimeTypeRegistry.RuntimeTypes)
        {
            builder.Entity(type);
        }
    }
}

public class RuntimeDbContext<T> : DbContext
    where T : class
{
    private readonly DatabaseProvider? _databaseProvider;
    private readonly string? _connectionString;

    public RuntimeDbContext(DbContextOptions<RuntimeDbContext<T>> options) : base(options)
    {
        _connectionString = null;
        _databaseProvider = null;
    }
    
    public RuntimeDbContext(DatabaseProvider databaseProvider, string connectionString)
    {
        _databaseProvider = databaseProvider;
        _connectionString = connectionString;
    }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        if(string.IsNullOrEmpty(_connectionString) || _databaseProvider == null)
            return;

        if (_databaseProvider == DatabaseProvider.MSSQL)
            optionsBuilder.UseSqlServer(_connectionString);

        if(_databaseProvider == DatabaseProvider.Sqlite)
            optionsBuilder.UseSqlite(_connectionString);
    }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        builder.Entity<T>();
    }
}