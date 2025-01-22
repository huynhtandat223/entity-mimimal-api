using CFW.ODataCore.Models.Deltas;

namespace CFW.ODataCore.Models.Requests;

public class CreationCommand<TEntity>
    where TEntity : class
{
    public EntityDelta<TEntity> Delta { get; init; }

    public CreationCommand(EntityDelta<TEntity> delta)
    {
        Delta = delta;
    }
}

public class PatchCommand<TEntity, TKey>
    where TEntity : class
{
    public TKey Key { get; init; }

    public EntityDelta<TEntity> Delta { get; init; }

    public PatchCommand(EntityDelta<TEntity> delta, TKey key)
    {
        Delta = delta;
        Key = key;
    }
}
