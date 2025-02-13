using CFW.EntityApi.Models.Builders;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

namespace CFW.EntityApi.Queries;

[Obsolete("Is this needed?")]
public interface IDbEntityApiCreationRouter : IContainerMemberRouter
{
    public IEntityType EntityType { get; set; }

    public IProperty KeyProperty { get; set; }

    public EntityApiConfiguration? EntityApiConfiguration { get; set; }
}

[Obsolete("Is this needed?")]
public interface IDbEntityApiCreationRouter<TDbContext> : IDbEntityApiCreationRouter
{
}

[Obsolete("Is this needed?")]
public interface IDbEntityApiCreationRouter<TDbContext, TEntity, TKey> : IDbEntityApiCreationRouter<TDbContext>
    where TDbContext : DbContext
    where TEntity : class
{
}
