using CFW.ODataCore.Intefaces;
using CFW.ODataCore.Models.Requests;

namespace CFW.ODataCore.DefaultHandlers;

public class EntityDeletionHandler<TEntity, TKey> : IEntityDeletionHandler<TEntity, TKey>
    where TEntity : class
{
    private readonly IServiceProvider _serviceProvider;

    public EntityDeletionHandler(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public async Task<Result> Handle(DeletionCommand<TEntity, TKey> command, CancellationToken cancellationToken)
    {
        var entityConfig = command.EntityConfiguration!;

        var result = await entityConfig.DeleteEntity(command.Key, _serviceProvider, cancellationToken);
        return result;
    }
}