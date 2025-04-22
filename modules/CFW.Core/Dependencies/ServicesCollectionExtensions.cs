using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace CFW.Core.Dependencies;
public static class ServicesCollectionExtensions
{
    public static IServiceCollection TryAddAllServices(this IServiceCollection services)
    {
        var scopeServices = AppDomain.CurrentDomain
            .GetAssemblies()
            .SelectMany(a => a.GetTypes())
            .Where(t => t.IsClass && !t.IsAbstract && typeof(IScopedService).IsAssignableFrom(t))
            .ToList();

        foreach (var service in scopeServices)
            services.TryAddScoped(service);

        return services;
    }
}
