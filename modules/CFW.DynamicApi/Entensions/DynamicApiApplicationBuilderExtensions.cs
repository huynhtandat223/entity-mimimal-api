using CFW.DynamicApi.Buiders;
using CFW.DynamicApi.Interceptors.OData;
using CFW.DynamicApi.OpenApiTransformers;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.OData.Formatter;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.OData;
using Scalar.AspNetCore;
using System.Reflection;
using System.Text;

namespace CFW.DynamicApi.Entensions;

/// <summary>
/// Flow: In app startup, dev pass routePrefix, container configuration action to config DefaultPageSize, ...etc
/// DI will be add singleton ContainerConfiguration instance after it execute container configuration action.
/// DI scan assemblies to find IEndpointConfigurator implementation and add transient  
///     -> When app.Buid:  DynamicEntityGroupBuilder builder intance create with purpose: contains authen, allowed properties, ... of groups 
///     
/// /// DI scan assemblies to find ApiOperationAttribute implementation and add transient 
///     -> 
/// </summary>

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
            o.AddDocumentTransformer<DocumentTransformer>();
            o.AddSchemaTransformer<OpenApiSchemaTransformer>();
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

        var types = assembliesToScan
            .SelectMany(a => a.GetTypes())
            .Where(t => t.IsClass && !t.IsAbstract)
            .ToList();

        //Register endpoint configurators - config endpoints in module initialier
        var endpointConfigurators = types
            .Where(x => typeof(IEndpointConfigurator).IsAssignableFrom(x))
            .ToList();

        foreach (var type in endpointConfigurators)
        {
            services.AddTransient(typeof(IEndpointConfigurator), type);
        }

        //Register api attributes - config endpoint in handler.
        var operationAttributes = types
            .Where(x => x.GetCustomAttribute<ApiOperationAttribute>() is not null)
            .Select(x => new
            {
                Attribute = x.GetCustomAttribute<ApiOperationAttribute>()!,
                Type = x
            })
            .ToList();
        foreach (var attr in operationAttributes)
        {
            attr.Attribute.TargetType = attr.Type;
        }
        services.AddSingleton(operationAttributes.Select(x => x.Attribute).ToList());

        // Use to init default paramter of IEndpointConfigurator instance.
        services.TryAddTransient(typeof(DynamicEntityGroupBuilder));
        services.TryAddTransient(typeof(DynamicEntityGroupBuilder<,>));
        services.TryAddTransient(typeof(DynamicEntityGroupBuilder<,,>));

        //Odata services interceptors
        services.TryAddTransient(typeof(ODataFeatureInterceptor<>));

        services.TryAddSingleton(_ =>
        {
            var formatter = new ODataOutputFormatter([ODataPayloadKind.ResourceSet]);
            formatter.SupportedEncodings.Add(Encoding.UTF8);

            return formatter;
        });

        services.TryAddSingleton<DynamicApiRegistry>();

        return services;
    }

    public static async Task<IApplicationBuilder> UseDynamicApi(this WebApplication app)
    {
        var serviceProvider = app.Services;
        using var scope = serviceProvider.CreateScope();

        var containerConfigs = scope.ServiceProvider.GetServices<ContainerConfiguration>();
        if (containerConfigs.Any() == false)
            throw new InvalidOperationException("No ContainerConfiguration found. Please call AddDynamicApi first.");

        var registry = scope.ServiceProvider.GetRequiredService<DynamicApiRegistry>();
        foreach (var containerConfig in containerConfigs)
        {
            var configurators = scope.ServiceProvider.GetServices<IEndpointConfigurator>();
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

            registry.MapApi(app, containerConfig);
        }

        app.MapOpenApi();
        app.MapScalarApiReference(_ => _.Servers = []);

        return app;
    }
}
