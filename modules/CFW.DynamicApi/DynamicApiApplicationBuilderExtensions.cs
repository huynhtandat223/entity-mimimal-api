using CFW.DynamicApi.Interceptors;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.OData.Formatter;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.OData;
using Scalar.AspNetCore;
using System.Reflection;
using System.Text;

namespace CFW.DynamicApi;

public static class DynamicApiApplicationBuilderExtensions
{
    public static IServiceCollection AddDynamicApi(
        this IServiceCollection services,
        string routePrefix,
        Action<ContainerConfiguration>? configureContainer = null,
        params Assembly[] assembliesToScan)
    {
        services.AddOpenApi(o =>
        {
            //o.AddOperationTransformer<OpenApiQueryOperationTransformer>();
        });

        var containerConfig = new ContainerConfiguration
        {
            RoutePrefix = routePrefix
        };

        configureContainer?.Invoke(containerConfig);

        services.AddSingleton(containerConfig);

        assembliesToScan = assembliesToScan.Any() == true
            ? assembliesToScan
            : [Assembly.GetEntryAssembly()!];

        var endpointConfigurators = assembliesToScan
            .SelectMany(a => a.GetTypes())
            .Where(t => t.IsClass && !t.IsAbstract
                     && typeof(IEndpointConfigurator).IsAssignableFrom(t))
            .ToList();

        foreach (var type in endpointConfigurators)
        {
            services.TryAddSingleton(typeof(IEndpointConfigurator), type);
        }

        //interceptors
        services.TryAddTransient(typeof(ODataFeatureInterceptor<>));

        //Odata services
        services.TryAddSingleton(_ =>
        {
            var formatter = new ODataOutputFormatter([ODataPayloadKind.ResourceSet]);
            formatter.SupportedEncodings.Add(Encoding.UTF8);

            return formatter;
        });

        return services;
    }

    public static async Task<IApplicationBuilder> UseDynamicApi(this WebApplication app)
    {
        var serviceProvider = app.Services;
        var containerConfigs = serviceProvider.GetServices<ContainerConfiguration>();
        if (containerConfigs.Any() == false)
            throw new InvalidOperationException("No ContainerConfiguration found. Please call AddDynamicApi first.");

        foreach (var containerConfig in containerConfigs)
        {
            var registry = new DynamicApiRegistry();

            var configurators = serviceProvider.GetServices<IEndpointConfigurator>();
            foreach (var configurator in configurators)
            {
                var builder = configurator.Configure();
                if (builder is null)
                {
                    builder = await configurator.ConfigureAsync();
                }
                builder.Build();
                registry.RegisterApiGroup(builder);
            }

            var dispatcher = new DynamicApiDispatcher(registry, containerConfig);
            dispatcher.MapEndpoints(app);
        }

        app.MapOpenApi();
        app.MapScalarApiReference(_ => _.Servers = []);

        return app;
    }
}
