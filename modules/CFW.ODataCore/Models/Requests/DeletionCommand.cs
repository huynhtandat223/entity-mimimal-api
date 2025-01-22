namespace CFW.ODataCore.Models.Requests;

public class DeletionCommand<TEntity, TKey>
    where TEntity : class
{
    public EntityEndpoint<TEntity>? EntityConfiguration { get; set; }

    public TKey Key { get; init; }

    public DeletionCommand(TKey key)
    {
        Key = key;
    }
}