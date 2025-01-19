using CFW.ODataCore.Intefaces;
using CFW.ODataCore.Models;
using CFW.ODataCore.Models.Deltas;
using CFW.ODataCore.Models.Requests;
using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace CFW.ODataCore.DefaultHandlers;

public class EntityCreationHandler<TEntity> : IEntityCreationHandler<TEntity>
    where TEntity : class, new()
{
    private readonly IDbContextProvider _dbContextProvider;

    public EntityCreationHandler(IDbContextProvider dbContextProvider)
    {
        _dbContextProvider = dbContextProvider;
    }

    public async Task<Result> Handle(CreationCommand<TEntity> command, CancellationToken cancellationToken)
    {
        var entity = command.Delta.Instance!;
        var db = _dbContextProvider.GetDbContext();
        var entry = db.Add(entity);

        await ProcessChangedNavigationPropertiesRecursive(command.Delta.ChangedProperties!, entry, cancellationToken);

        var affected = await db.SaveChangesAsync(cancellationToken);
        if (affected == 0)
        {
            return entity.Failed("Failed to create entity");
        }

        return entity.Created();
    }

    private async Task ProcessChangedNavigationPropertiesRecursive(
        IDictionary<string, object> changedProperties,
        EntityEntry entry,
        CancellationToken cancellationToken)
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