using CFW.ODataCore.Intefaces;
using CFW.ODataCore.Models.Requests;

namespace CFW.ODataCore.DefaultHandlers;

public class EntityPatchHandler<TEntity, TKey> : IEntityPatchHandler<TEntity, TKey>
    where TEntity : class
{
    private readonly IServiceProvider _serviceProvider;

    public EntityPatchHandler(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public async Task<Result> Handle(PatchCommand<TEntity, TKey> command, CancellationToken cancellationToken)
    {
        var entityConfig = command.Delta.EntityConfiguration!;

        var result = await entityConfig.PatchEntity(command.Delta, command.Key, _serviceProvider, cancellationToken);
        return result;
    }
}
