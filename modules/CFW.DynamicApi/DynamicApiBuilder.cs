using Microsoft.EntityFrameworkCore;

namespace CFW.DynamicApi;

public class DynamicApiBuilder
{
    internal ContainerConfiguration ContainerConfig { get; }
    internal DynamicApiRegistry ApiRegistry { get; }

    public DynamicApiBuilder(DynamicApiRegistry apiRegistry, ContainerConfiguration containerConfig)
    {
        ContainerConfig = containerConfig;
        ApiRegistry = apiRegistry;
    }

    //public DynamicEntityGroupBuilder<TEntity> AddRouteGroup<TEntity>(Action<DynamicEntityGroupBuilder<TEntity>>? configure = null)
    //    where TEntity : class
    //{
    //    var builder = DynamicEntityGroupBuilder<TEntity>.Create();
    //    configure?.Invoke(builder);

    //    ApiRegistry.RegisterOperations(builder.Build());
    //    return builder;
    //}

    public DynamicDbSetEntityGroupBuilder<TEntity, TDbContext, TKey> AddDbSetRouteGroup<TEntity, TDbContext, TKey>(Action<DynamicDbSetEntityGroupBuilder<TEntity, TDbContext, TKey>>? configure = null)
        where TEntity : class
        where TDbContext : DbContext
    {
        var builder = DynamicDbSetEntityGroupBuilder<TEntity, TDbContext, TKey>.Create();
        configure?.Invoke(builder);

        ApiRegistry.RegisterOperations(builder.Build());

        return builder;
    }

    //public DynamicApiBuilder AddOperation(Action<DynamicOperationBuilder> configure)
    //{
    //    var builder = new DynamicOperationBuilder();
    //    configure(builder);

    //    ApiRegistry.RegisterOperations([builder.Build()]);

    //    return this;
    //}
}
