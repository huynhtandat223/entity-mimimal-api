using CFW.Core.Utils;
using CFW.EntityApi.Models;
using CFW.EntityApi.Models.Builders;
using CFW.EntityApi.Registrators;
using Microsoft.AspNetCore.OData.Formatter;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.OData;
using System.Text;

namespace CFW.EntityApi;

public static class ServicesCollectionExtensions
{
    public static ContainerApiBuilder AddEntityMinimalApi(this IServiceCollection services, string defaultRoutePrefix)
    {
        //OData services
        services.TryAddSingleton(_ =>
        {
            var formatter = new ODataOutputFormatter([ODataPayloadKind.ResourceSet]);
            formatter.SupportedEncodings.Add(Encoding.UTF8);

            return formatter;
        });

        var sanitizeRoute = StringUtils.SanitizeRoute(defaultRoutePrefix);
        var builder = new ContainerApiBuilder(sanitizeRoute, services);

        services.AddSingleton(builder.ContainerConfiguration);

        var configurationInterface = typeof(IEntityApiConfiguration<>);

        var configurationTypes = AppDomain.CurrentDomain.GetAssemblies()
            .SelectMany(assembly => assembly.GetTypes())
            .Where(type => !type.IsInterface && !type.IsAbstract)
            .Where(type => type.GetInterfaces()
                .Any(i => i.IsGenericType && i.GetGenericTypeDefinition() == configurationInterface))
            .ToList();

        foreach (var implementationType in configurationTypes)
        {
            // Assuming each implementation only implements one IEntityApiConfiguration<T>
            var interfaceTypes = implementationType.GetInterfaces()
                .Where(i => i.IsGenericType && i.GetGenericTypeDefinition() == configurationInterface);

            foreach (var interfaceType in interfaceTypes)
            {
                var entityType = interfaceType.GetGenericArguments()[0];
                builder.ContainerConfiguration.CustomEntityImplementations.Add(entityType);

                services.AddSingleton(interfaceType, implementationType);

                var apiConfiguration = typeof(EntityApiConfiguration<>)
                    .MakeGenericType(entityType);

                services.AddSingleton(typeof(EntityApiConfiguration), apiConfiguration);
            }
        }

        return builder;
    }

    public static WebApplication UseEntityMinimalApi(this WebApplication app)
    {
        var containerConfigurations = app.Services.GetServices<ContainerConfiguration>();

        foreach (var containerConfiguration in containerConfigurations)
        {
            var typeResolver = app.Services.GetService<ITypesResolver>()!;
            if (typeResolver == null)
                typeResolver = new DefaultTypesResolver(app.Services, containerConfiguration);
            var containerRegistrationContext
                = new ContainerRegistrationContext(app.Services, containerConfiguration, typeResolver);

            containerRegistrationContext.ContainerGroupRoute = app.MapGroup(containerConfiguration.RoutePrefix);
            containerConfiguration.ConfigureContainerRouteGroup?.Invoke(containerRegistrationContext.ContainerGroupRoute);

            foreach (var feature in containerConfiguration.ApiFeatures)
            {
                feature.Register(containerRegistrationContext);
            }

            var actionRouter = ActivatorUtilities.CreateInstance<Actions.Route>(app.Services)!;
            actionRouter.Register(containerRegistrationContext.ContainerGroupRoute, typeResolver.ActionAttributes);
        }

        return app;
    }
}
