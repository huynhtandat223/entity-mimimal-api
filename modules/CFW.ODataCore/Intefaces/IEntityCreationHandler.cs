using CFW.ODataCore.Models.Requests;

namespace CFW.ODataCore.Intefaces;

public interface IEntityCreationHandler<TEntity>
    where TEntity : class
{
    Task<Result> Handle(CreationCommand<TEntity> command, CancellationToken cancellationToken);
}

public interface IEntityPatchHandler<TEntity, TKey>
    where TEntity : class
{
    Task<Result> Handle(PatchCommand<TEntity, TKey> command, CancellationToken cancellationToken);
}