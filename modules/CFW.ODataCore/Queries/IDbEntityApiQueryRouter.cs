using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

namespace CFW.EntityApi.Queries;

public interface IDbEntityApiQueryRouter : IContainerMemberRouter
{
    public IEntityType EntityType { get; set; }

    public IProperty KeyProperty { get; set; }
}

public interface IDbEntityApiQueryRouter<TDbContext> : IDbEntityApiQueryRouter
{
}

public interface IDbEntityApiQueryRouter<TDbContext, TEntity, TKey> : IDbEntityApiQueryRouter<TDbContext>
    where TDbContext : DbContext
    where TEntity : class
{

}
