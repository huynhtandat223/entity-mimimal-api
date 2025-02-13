using CFW.EntityApi.Models.Builders;
using CFW.EntityApi.Models.Deltas;
using CFW.EntityApi.Registrators;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata;

namespace CFW.EntityApi.Queries;

public class DefaultEntityApiCreationRouter<TDbContext, TEntity, TKey>
    : IDbEntityApiCreationRouter<TDbContext, TEntity, TKey>
where TDbContext : DbContext
where TEntity : class
{
    public IEntityType EntityType { get; set; } = null!;

    public IProperty KeyProperty { get; set; } = null!;

    public EntityApiConfiguration? EntityApiConfiguration { get; set; }


    public Task Register(ContainerMemberRegistrationContext containerMemberRegistrationContext)
    {
        var entityGroupBuider = containerMemberRegistrationContext.MemberRouteGroup;
        var creationFactory = EntityApiConfiguration?.CreationFactory;

        if (creationFactory is null)
        {
            creationFactory = async (sp, delta) =>
            {
                if (delta is not EntityDelta<TEntity> entityDelta)
                {
                    throw new InvalidOperationException();
                }

                var db = sp.GetRequiredService<TDbContext>();
                var entity = entityDelta.Instance!;
                var entry = db!.Set<TEntity>().Add(entity);

                await ProcessChangedNavigationPropertiesRecursive(delta.ChangedProperties!, entry, default);

                var affected = await db.SaveChangesAsync();
                if (affected == 0)
                {
                    return entity.Failed("Failed to create entity");
                }

                return entity.Created();
            };
        }

        entityGroupBuider.MapPost("/", async (EntityDelta<TEntity> delta
            , [FromServices] IServiceProvider sp
            , CancellationToken cancellationToken) =>
        {
            var result = await creationFactory(sp, delta);
            return result.ToResults();
        });

        return Task.CompletedTask;
    }

    private async Task ProcessChangedNavigationPropertiesRecursive(
        IDictionary<string, object> changedProperties,
        EntityEntry entry,
        CancellationToken cancellationToken = default)
    {
        var entityDeltas = changedProperties
            .Where(x => x.Value is EntityDelta delta && delta.EfCoreEntityType is not null);

        foreach (var (key, value) in entityDeltas)
        {
            var delta = (EntityDelta)value;
            var navigation = entry.Navigation(key);
            if (!navigation.IsLoaded)
            {
                await navigation.LoadAsync(cancellationToken);
            }
            await ProcessChangedNavigationPropertiesRecursive(delta.ChangedProperties!
                , entry.Context.Entry(navigation.CurrentValue!), cancellationToken);
        }

        var collectionDeltas = changedProperties
            .Where(x => x.Value is EntityDeltaSet deltaSets);

        foreach (var (key, value) in collectionDeltas)
        {
            var deltaSets = (EntityDeltaSet)value;
            var navigation = entry.Navigation(key);
            if (!navigation.IsLoaded)
            {
                await navigation.LoadAsync(cancellationToken);
            }

            foreach (var delta in deltaSets.ChangedProperties)
            {
                var itemEntry = entry.Context.Entry(delta.GetInstance()!);
                await ProcessChangedNavigationPropertiesRecursive(delta!.ChangedProperties!
                    , itemEntry, cancellationToken);
            }
        }
    }
}