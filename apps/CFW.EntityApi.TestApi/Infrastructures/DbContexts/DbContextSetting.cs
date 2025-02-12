using Microsoft.EntityFrameworkCore;

namespace CFW.EntityApi.TestApi.Infrastructures.DbContexts;

public class DbContextSetting
{
    public string? SqliteConnectionString { get; set; }

    public string? SqlServerConnectionString { get; set; }

    public void Configure(DbContextOptionsBuilder options)
    {
        if (SqliteConnectionString != null)
        {
            options.UseSqlite(SqliteConnectionString);
        }
        else if (SqlServerConnectionString != null)
        {
            //options.UseSqlServer(SqlServerConnectionString);
        }
    }
}
