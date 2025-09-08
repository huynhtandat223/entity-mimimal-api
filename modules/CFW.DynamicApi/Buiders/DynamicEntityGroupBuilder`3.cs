using CFW.Core.Results;
using CFW.Core.Utils;
using CFW.DynamicApi.Deltas;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace CFW.DynamicApi.Buiders;

[Obsolete("Is need to refactor or simplify ????")]
public class DynamicEntityGroupBuilder<TEntity, TDbContext, TKey> : DynamicEntityGroupBuilder<TEntity, TDbContext>
    where TEntity : class
    where TDbContext : DbContext
    where TKey : notnull
{
    public DynamicEntityGroupBuilder(TDbContext db) : base(db)
    {
    }

    public DynamicEntityGroupBuilder<TEntity, TDbContext, TKey> AddUpdatingApi(Action<DynamicApiOperation>? operationConfig = null)
    {
        var result = new DynamicApiOperation<TKey>
        {
            Route = $"/{{key}}",
            EntityGroup = this,
            HttpMethod = HttpMethod.Put.Method,
            ModelHandler = async (context, key) =>
            {
                var db = context.RequestServices.GetRequiredService<TDbContext>();
                var dbEntity = await db.Set<TEntity>().FindAsync(key);

                if (dbEntity is null)
                {
                    return this.Notfound();
                }

                var delta = await EntityDelta<TEntity>.BindAsync(context);
                var apiOperation = context.GetEndpoint()!.Metadata.GetMetadata<DynamicApiOperation>()!;

                foreach (var prop in apiOperation.AllowedProperties)
                {
                    db.Entry(dbEntity).Property(prop.Name).IsModified = true;
                    db.Entry(dbEntity).Property(prop.Name).CurrentValue = delta!.ChangedProperties[prop.Name];
                }

                await ProcessChangedNavigationPropertiesRecursive(delta!.ChangedProperties!, db.Entry(dbEntity), default);

                var affected = await db.SaveChangesAsync();
                if (affected == 0)
                {
                    return this.Failed("Failed to update entity");
                }

                return this.Success();
            }
        };

        operationConfig?.Invoke(result);

        WithOperation(result);
        return this;
    }

    public DynamicEntityGroupBuilder<TEntity, TDbContext, TKey> AddPartialUpdatingApi(Action<DynamicApiOperation>? operationConfig = null)
    {
        var result = new DynamicApiOperation<TKey>
        {
            Route = $"/{{key}}",
            EntityGroup = this,
            HttpMethod = HttpMethod.Patch.Method,
            ModelHandler = async (context, key) =>
            {
                var db = context.RequestServices.GetRequiredService<TDbContext>();
                var dbEntity = await db.Set<TEntity>().FindAsync(key);

                if (dbEntity is null)
                {
                    return this.Notfound();
                }

                var delta = await EntityDelta<TEntity>.BindAsync(context);
                var apiOperation = context.GetEndpoint()!.Metadata.GetMetadata<DynamicApiOperation>()!;

                foreach (var changedProp in delta!.ChangedProperties.Keys)
                {
                    if (apiOperation.AllowedProperties.All(x => x.Name != changedProp))
                    {
                        return this.Failed($"Property {changedProp} is not allowed to be updated");
                    }

                    db.Entry(dbEntity).Property(changedProp).IsModified = true;
                    db.Entry(dbEntity).Property(changedProp).CurrentValue = delta.ChangedProperties[changedProp];
                }

                await ProcessChangedNavigationPropertiesRecursive(delta!.ChangedProperties!, db.Entry(dbEntity), default);

                var affected = await db.SaveChangesAsync();
                if (affected == 0)
                {
                    return this.Failed("Failed to update entity");
                }

                return this.Success();
            }
        };

        operationConfig?.Invoke(result);

        WithOperation(result);
        return this;
    }

    public DynamicEntityGroupBuilder<TEntity, TDbContext, TKey> AddDeleteApi(Action<DynamicApiOperation>? operationConfig = null)
    {
        var result = new DynamicApiOperation<TKey>
        {
            Route = $"/{{key}}",
            EntityGroup = this,
            HttpMethod = HttpMethod.Delete.Method,
            ModelHandler = async (context, key) =>
            {
                var db = context.RequestServices.GetRequiredService<TDbContext>();
                var dbEntity = await db.Set<TEntity>().FindAsync(key);

                if (dbEntity is null)
                {
                    return this.Notfound();
                }

                db.Set<TEntity>().Remove(dbEntity);

                var affected = await db.SaveChangesAsync();
                if (affected == 0)
                {
                    return this.Failed("Failed to update entity");
                }

                return this.Success();
            }
        };

        operationConfig?.Invoke(result);

        WithOperation(result);
        return this;
    }

    public DynamicEntityGroupBuilder<TEntity, TDbContext, TKey> AddGetSingleApi(Action<DynamicApiOperation>? operationConfig = null)
    {
        var result = new DynamicApiOperation<TKey>
        {
            Route = $"/{{key}}",
            EntityGroup = this,
            HttpMethod = HttpMethod.Get.Method,
            ModelHandler = async (context, key) =>
            {
                var db = context.RequestServices.GetRequiredService<TDbContext>();
                var apiOperation = context.GetEndpoint()!.Metadata.GetMetadata<DynamicApiOperation>()!;
                var propertiesToSelect = apiOperation.AllowedProperties
                    .Select(p => p.Name)
                    .ToArray();

                var keyProp = apiOperation.EntityGroup.Properties!.Single(x => x.IsKey);
                var selector = ExpressionUtils.BuildSelector<TEntity>(propertiesToSelect);
                var keyEquality = ExpressionUtils.BuilderEqualExpression<TEntity>(key, keyProp.Name);
                var singleElement = db.Set<TEntity>().Where(keyEquality).Select(selector);

                return await Task.FromResult(singleElement);
            }
        };

        operationConfig?.Invoke(result);

        WithOperation(result);
        return this;
    }
}