using CFW.ODataCore.Intefaces;
using CFW.ODataCore.Models.Requests;

namespace CFW.ODataCore.DefaultHandlers;

public class EntityCreationHandler<TEntity> : IEntityCreationHandler<TEntity>
    where TEntity : class
{
    private readonly IServiceProvider _serviceProvider;

    public EntityCreationHandler(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public async Task<Result> Handle(CreationCommand<TEntity> command, CancellationToken cancellationToken)
    {
        var entity = command.Delta.Instance!;
        var entityConfig = command.Delta.EntityConfiguration!;

        var result = await entityConfig.CreateEntity(command.Delta, _serviceProvider, cancellationToken);
        return result;

    }
}