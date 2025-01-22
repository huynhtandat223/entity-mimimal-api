using CFW.ODataCore.Models.Requests;

namespace CFW.ODataCore.Intefaces;

public interface IEntityPatchHandler<TEntity, TKey>
    where TEntity : class
{
    Task<Result> Handle(PatchCommand<TEntity, TKey> command, CancellationToken cancellationToken);
}

public interface IEntityDeletionHandler<TEntity, TKey>
    where TEntity : class
{
    Task<Result> Handle(DeletionCommand<TEntity, TKey> command, CancellationToken cancellationToken);
}