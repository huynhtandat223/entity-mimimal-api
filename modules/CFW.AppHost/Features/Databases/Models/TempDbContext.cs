using Microsoft.EntityFrameworkCore;

namespace CFW.AppHost.Features.Databases.Models;

public class TempDbContext : DbContext
{
    public TempDbContext(DbContextOptions<TempDbContext> options) : base(options)
    {
    }
}
