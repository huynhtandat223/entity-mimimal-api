using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace CFW.DynamicApi;

public class DynamicDbSetEntityGroupBuilder<TEntity, TDbContext, TKey> : DynamicEntityGroupBuilder<TEntity>
    where TEntity : class
    where TDbContext : DbContext
{
    private DynamicDbSetEntityGroupBuilder() { }

    public static DynamicDbSetEntityGroupBuilder<TEntity, TDbContext, TKey> Create() => new();

    public override DynamicEntityGroupBuilder<TEntity> AllowListing(Action<DynamicOperationBuilder>? configure = null)
    {
        var builder = new DynamicOperationBuilder()
            .WithMethod(HttpMethods.Get)
            .WithRoute("/");

        builder.Handle(async ctx =>
        {
            var db = ctx.RequestServices.GetRequiredService<TDbContext>();
            var query = db.Set<TEntity>().AsNoTracking().AsQueryable();
            return query;
        });

        configure?.Invoke(builder);

        _operations.Add(builder.Build());
        return this;
    }
}
