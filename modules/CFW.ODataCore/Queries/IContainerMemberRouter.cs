using CFW.EntityApi.Registrators;

namespace CFW.EntityApi.Queries;

public interface IContainerMemberRouter
{
    public Task Register(ContainerMemberRegistrationContext containerMemberRegistrationContext);
}

public interface IEntityApiQueryRouter<TEntity>
    where TEntity : class
{
    public Task Register(ContainerMemberRegistrationContext containerMemberRegistrationContext);
}
