using CFW.ODataCore.Models.Deltas;

namespace CFW.ODataCore.Models.Requests;

public class CreationCommand<TEntity>
    where TEntity : class, new()
{
    public EntityDelta<TEntity> Delta { get; init; }

    public CreationCommand(EntityDelta<TEntity> delta)
    {
        Delta = delta;
    }
}
