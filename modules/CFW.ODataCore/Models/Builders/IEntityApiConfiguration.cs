using CFW.EntityApi.Attributes;

namespace CFW.EntityApi.Models.Builders;

public interface IEntityApiConfiguration<TEntity>
    where TEntity : class
{
    Task Configure(EntityApiConfigurationBuilder<TEntity> builder);
}

public class AttributeEntityApiConfiguration<TEntity> : IEntityApiConfiguration<TEntity>
    where TEntity : class
{
    private readonly EntityAttribute _entityAttribute;
    public AttributeEntityApiConfiguration(EntityAttribute entityAttribute)
    {
        _entityAttribute = entityAttribute;
    }
    public Task Configure(EntityApiConfigurationBuilder<TEntity> builder)
    {
        var name = _entityAttribute.Name ?? typeof(TEntity).Name;

        builder
            .UseName(name)
            .UseQuery<DefaultDbContext>(db => db.Set<TEntity>().AsQueryable());

        return Task.CompletedTask;
    }
}
