using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace CFW.DynamicApi.Entensions;
public static class RequestModelExtenstions
{
    public static async Task<TEntity> CreateEntity<TEntity, TDbContext>(this RequestModel<TEntity> requestModel)
        where TEntity : class
        where TDbContext : DbContext
    {
        var db = requestModel.ServiceProvider.GetRequiredService<TDbContext>();
        var entity = requestModel.Model;
        db.Set<TEntity>().Add(entity);
        await db.SaveChangesAsync();
        return entity;
    }
}
