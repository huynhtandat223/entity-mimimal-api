using Microsoft.EntityFrameworkCore;

namespace CFW.AppHost.Infrastructures.DbContextExtensions.Models;

public class TempDbContext : DbContext
{
    public TempDbContext(DbContextOptions<TempDbContext> options) : base(options)
    {
    }
}
