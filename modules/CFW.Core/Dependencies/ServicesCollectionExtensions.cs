using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using System.Reflection;

namespace CFW.Core.Dependencies;

public static class ServicesCollectionExtensions
{
    public static async Task<IHostApplicationBuilder> TryAddAllServicesAndInitModules(this IHostApplicationBuilder builder)
    {
        await Task.CompletedTask;

        var cacheTypes = AppDomain.CurrentDomain
            .GetAssemblies()
            .SelectMany(a => a.GetTypes())
            .Where(t => t.IsClass && !t.IsAbstract)
            .ToList();

        var scopeServices = cacheTypes
            .Where(t => typeof(IScopedService).IsAssignableFrom(t))
            .ToList();

        var services = builder.Services;
        var configuration = builder.Configuration;
        foreach (var service in scopeServices)
            services.TryAddScoped(service);

        var configTypes = cacheTypes.Where(x => x.GetCustomAttribute<SectionConfigAttribute>() is not null).ToList();
        foreach (var configType in configTypes)
        {
            var attr = configType.GetCustomAttribute<SectionConfigAttribute>()!;
            attr.Configure(services, configuration);
        }

        var moduleTypes = cacheTypes
            .Where(t => typeof(IModuleInitializer).IsAssignableFrom(t))
            .ToList();

        var servieProvider = services.BuildServiceProvider(false);
        foreach (var moduleType in moduleTypes)
        {
            using var scope = servieProvider.CreateScope();
            var moduleInstance = ActivatorUtilities
                .CreateInstance(scope.ServiceProvider, moduleType) as IModuleInitializer;
            await moduleInstance!.InitModule(builder);

            services.AddScoped(typeof(IModuleInitializer), moduleType);
        }

        return builder;
    }

    public static async Task<IHost> RunModules(this IHost host)
    {
        using var scope = host.Services.CreateScope();
        var modules = scope.ServiceProvider.GetServices<IModuleInitializer>();
        foreach (var module in modules)
        {
            await module.RunModule(host);
        }

        return host;
    }
}
