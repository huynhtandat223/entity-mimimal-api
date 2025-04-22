using CFW.EntityApi.Registrators;
using Microsoft.AspNetCore.OData;
using Microsoft.AspNetCore.OData.Query;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.OData.ModelBuilder;

namespace CFW.EntityApi.Models.Builders;

public class ContainerApiBuilder
{
    private readonly ContainerConfiguration _containerConfiguration = new ContainerConfiguration();
    private readonly IServiceCollection _services;

    public ContainerApiBuilder(string defaultRoutePrefix, IServiceCollection services)
    {
        _services = services;
        _containerConfiguration.RoutePrefix = defaultRoutePrefix;

        services.AddOptions<ODataOptions>(_containerConfiguration.RoutePrefix);
    }

    internal ContainerConfiguration ContainerConfiguration => _containerConfiguration;

    public ContainerApiBuilder SetDefault(bool isDefault = true)
    {
        _containerConfiguration.IsDefault = isDefault;
        return this;
    }

    public ContainerApiBuilder ConfigureODataModelBuilder(Action<ODataConventionModelBuilder> configureModelBuilder)
    {
        _containerConfiguration.ConfigureModelBuilder = configureModelBuilder;

        return this;
    }

    public ContainerApiBuilder SetDefaultPageSize(int defaultPageSize)
    {
        _containerConfiguration.DefaultPageSize = defaultPageSize;
        return this;
    }

    /// <summary>
    /// Configure EnableQueryFeatures use <see cref="ConfigureAllowQueryOptions(Func{AllowedQueryOptions})"/>
    /// </summary>
    /// <param name="configureODataOptions"></param>
    /// <returns></returns>
    public ContainerApiBuilder ConfigureODataOptions(Action<ODataOptions> configureODataOptions)
    {
        _containerConfiguration.ConfigureODataOptions = configureODataOptions;
        return this;
    }

    public ContainerApiBuilder ConfigureAllowQueryOptions(AllowedQueryOptions allowedQueryOptions)
    {
        _containerConfiguration.AllowedQueryOptions = allowedQueryOptions;

        return this;
    }

    public ContainerApiBuilder ConfigureContainerRoute(Action<RouteGroupBuilder> configureContainerRouteGroup)
    {
        _containerConfiguration.ConfigureContainerRouteGroup = configureContainerRouteGroup;
        return this;
    }

    public ContainerApiBuilder AddApiConfigurations<TDependencyService>(
        Func<TDependencyService, IEnumerable<EntityApiConfiguration>> apiConfigurationsFunc)
        where TDependencyService : class
    {
        _containerConfiguration.ApiConfigurationsFunc = s =>
        {
            var dependencyService = s.GetRequiredService<TDependencyService>()!;
            return apiConfigurationsFunc(dependencyService);
        };
        return this;
    }

    public EntityFrameworkPopuplationFeature<TDbContext> PopuplateEntityFrameworkEntities<TDbContext>(
        Action<DbContextOptionsBuilder>? optionBuider = null,
        Func<IEntityType, bool>? entitiesSelector = null)
        where TDbContext : DbContext
    {
        if (optionBuider is not null)
        {
            _services.AddDbContext<TDbContext>(options =>
            {
                options.EnableSensitiveDataLogging()
                   .ReplaceService<IModelCustomizer, AutoScanModelCustomizer<TDbContext>>();

                optionBuider(options);
            });
        }

        var feature = new EntityFrameworkPopuplationFeature<TDbContext>(_containerConfiguration);
        feature.EntitiesSelector = entitiesSelector ?? (x => true);
        return feature;
    }

}
