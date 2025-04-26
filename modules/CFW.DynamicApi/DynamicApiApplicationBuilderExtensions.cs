using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;

namespace CFW.DynamicApi;

public static class DynamicApiApplicationBuilderExtensions
{
    public static IServiceCollection AddDynamicApi(
        this IServiceCollection services,
        string routePrefix,
        Action<ContainerConfiguration>? configureContainer = null,
        params Assembly[] assembliesToScan)
    {
        var containerConfig = new ContainerConfiguration
        {
            RoutePrefix = routePrefix
        };

        configureContainer?.Invoke(containerConfig);

        services.AddSingleton(containerConfig);

        if (assembliesToScan == null || assembliesToScan.Length == 0)
        {
            assembliesToScan = new[] { Assembly.GetEntryAssembly()! };
        }

        var endpointConfigurators = assembliesToScan
            .SelectMany(a => a.GetTypes())
            .Where(t => t.IsClass && !t.IsAbstract
                     && typeof(IEndpointConfigurator).IsAssignableFrom(t))
            .ToList();

        foreach (var type in endpointConfigurators)
        {
            services.AddSingleton(typeof(IEndpointConfigurator), type);
        }

        services.AddSingleton<DynamicApiRegistry>();
        services.AddSingleton<DynamicApiDispatcher>();

        services.AddTransient(typeof(ODataFeatureInterceptor<>));

        return services;
    }

    public static IApplicationBuilder UseDynamicApi(this IApplicationBuilder app)
    {
        var serviceProvider = app.ApplicationServices;

        var registry = serviceProvider.GetRequiredService<DynamicApiRegistry>();
        var containerConfig = serviceProvider.GetRequiredService<ContainerConfiguration>();

        // Create a "manual" builder for calling configurators
        var builder = new InternalDynamicApiBuilder(registry, containerConfig);

        var configurators = serviceProvider.GetServices<IEndpointConfigurator>();
        foreach (var configurator in configurators)
        {
            configurator.Configure(builder);
        }

        var dispatcher = serviceProvider.GetRequiredService<DynamicApiDispatcher>();
        dispatcher.MapEndpoints((IEndpointRouteBuilder)app);

        return app;
    }

    private class InternalDynamicApiBuilder : DynamicApiBuilder
    {
        private readonly DynamicApiRegistry _registry;

        public InternalDynamicApiBuilder(DynamicApiRegistry registry, ContainerConfiguration containerConfig)
            : base(registry, containerConfig)
        {
            _registry = registry;
        }

        public new DynamicEntityGroupBuilder<TEntity> AddRouteGroup<TEntity>(Action<DynamicEntityGroupBuilder<TEntity>>? configure = null)
            where TEntity : class
        {
            var builder = DynamicEntityGroupBuilder<TEntity>.Create();
            configure?.Invoke(builder);
            _registry.RegisterOperations(builder.Build());
            return builder;
        }

        public new DynamicDbSetEntityGroupBuilder<TEntity, TDbContext, TKey> AddDbSetRouteGroup<TEntity, TDbContext, TKey>(Action<DynamicDbSetEntityGroupBuilder<TEntity, TDbContext, TKey>>? configure = null)
            where TEntity : class
            where TDbContext : DbContext
        {
            var builder = DynamicDbSetEntityGroupBuilder<TEntity, TDbContext, TKey>.Create();
            configure?.Invoke(builder);
            _registry.RegisterOperations(builder.Build());
            return builder;
        }

        public new DynamicApiBuilder AddOperation(Action<DynamicOperationBuilder> configure)
        {
            var builder = new DynamicOperationBuilder();
            configure(builder);
            _registry.RegisterOperations(new[] { builder.Build() });
            return this;
        }
    }
}
