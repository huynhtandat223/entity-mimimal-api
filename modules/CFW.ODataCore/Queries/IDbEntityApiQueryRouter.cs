using CFW.EntityApi.Models.Builders;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

namespace CFW.EntityApi.Queries;

[Obsolete("Is this needed?")]
public interface IDbEntityApiQueryRouter : IContainerMemberRouter
{
    public IEntityType EntityType { get; set; }

    public IProperty KeyProperty { get; set; }

    public EntityApiConfiguration? EntityApiBuilder { get; set; }
}

[Obsolete("Is this needed?")]
public interface IDbEntityApiQueryRouter<TDbContext> : IDbEntityApiQueryRouter
{
}

[Obsolete("Is this needed?")]
public interface IDbEntityApiQueryRouter<TDbContext, TEntity, TKey> : IDbEntityApiQueryRouter<TDbContext>
    where TDbContext : DbContext
    where TEntity : class
{
}
