using CFW.ODataCore.Models.Requests;

namespace CFW.ODataCore.Intefaces;

public interface IEntityCreationHandler<TEntity>
    where TEntity : class
{
    Task<Result> Handle(CreationCommand<TEntity> command, CancellationToken cancellationToken);
}

