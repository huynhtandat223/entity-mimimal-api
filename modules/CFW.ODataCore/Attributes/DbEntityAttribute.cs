namespace CFW.EntityApi.Attributes;

public abstract class DbEntityAttribute : EntityAttribute
{
    public Type DbContextType { get; protected set; } = null!;

    public Type EntityType { get; protected set; } = null!;

    public Type KeyType { get; protected set; } = null!;

    public DbEntityAttribute(string? name = null) : base(name)
    {

    }
}

public class DbEntityAttribute<TDbContext, TEntity, TKey> : DbEntityAttribute
{
    public DbEntityAttribute(string? name = null) : base(name)
    {
        DbContextType = typeof(TDbContext);
        EntityType = typeof(TEntity);
        KeyType = typeof(TKey);
    }
}