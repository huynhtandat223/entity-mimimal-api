using CFW.EntityApi.Registrators;
using Microsoft.EntityFrameworkCore;
using Microsoft.OData.ModelBuilder;

namespace CFW.EntityApi.Models.Builders;

public class EntityApiBuilder
{
    private readonly ContainerConfiguration _containerConfiguration = new ContainerConfiguration();
    private readonly IServiceCollection _services;

    public EntityApiBuilder(string defaultRoutePrefix, IServiceCollection services)
    {
        _containerConfiguration.RoutePrefix = defaultRoutePrefix;
        _services = services;
    }

    public ContainerConfiguration containerRegistrator => _containerConfiguration;

    public EntityApiBuilder SetDefault(bool isDefault = true)
    {
        _containerConfiguration.IsDefault = isDefault;
        return this;
    }

    public EntityApiBuilder ConfigureODataModelBuilder(Action<ODataConventionModelBuilder> configureModelBuilder)
    {
        _containerConfiguration.ConfigureModelBuilder = configureModelBuilder;

        return this;
    }

    public EntityApiBuilder ConfigureContainerRoute(Action<RouteGroupBuilder> configureContainerRouteGroup)
    {
        _containerConfiguration.ConfigureContainerRouteGroup = configureContainerRouteGroup;
        return this;
    }

    public EntityAttributeApiBuilder UseEntityAttributes()
    {
        var entityAttributeBuilder = new EntityAttributeApiBuilder();
        _services.AddKeyedSingleton<IEntityMemberApiBuilder>(_containerConfiguration.RoutePrefix, entityAttributeBuilder);

        return entityAttributeBuilder;
    }

    public DbContextApiBuilder<TDbContext> UseDbContext<TDbContext>()
        where TDbContext : DbContext
    {
        var dbBuilder = new DbContextApiBuilder<TDbContext>();

        _services.AddKeyedSingleton<IEntityMemberApiBuilder>(_containerConfiguration.RoutePrefix, dbBuilder);
        return dbBuilder;
    }
}
