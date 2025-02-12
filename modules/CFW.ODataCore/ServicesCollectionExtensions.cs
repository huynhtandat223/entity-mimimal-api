using CFW.EntityApi.Models;
using CFW.EntityApi.Models.Builders;
using CFW.EntityApi.Queries;
using CFW.EntityApi.Registrators;
using Microsoft.AspNetCore.OData.Formatter;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.OData;
using Microsoft.OData.ModelBuilder;
using System.Text;

namespace CFW.EntityApi;

public static class ServicesCollectionExtensions
{
    public static EntityApiBuilder AddEntityApi(this IServiceCollection services, string defaultRoutePrefix)
    {
        //OData services
        var formatter = new ODataOutputFormatter([ODataPayloadKind.ResourceSet]);
        formatter.SupportedEncodings.Add(Encoding.UTF8);
        services.TryAddSingleton(formatter);

        //cached types
        services.TryAddSingleton<ITypesResolver, EntityApiAssemblyResolver>();
        //TODO: verify if this is correct for cached OData types purpose
        services.TryAddSingleton<IAssemblyResolver>(s => s.GetRequiredService<ITypesResolver>());

        services.TryAddSingleton(typeof(IDbEntityApiQueryRouter<,,>), typeof(DefaultDbEntityApiQueryRouter<,,>));
        services.TryAddSingleton(typeof(IEntityApiQuery<>), typeof(DefaultEntityApiQueryRouter<>));

        var builder = new EntityApiBuilder(defaultRoutePrefix, services);
        services.AddSingleton(builder.containerRegistrator);

        return builder;
    }

    public static WebApplication UseEntityApi(this WebApplication app)
    {
        var containerConfigurations = app.Services.GetServices<ContainerConfiguration>();
        var typeResolver = app.Services.GetRequiredService<ITypesResolver>();

        foreach (var containerConfiguration in containerConfigurations)
        {
            var containerRegistrationContext = new ContainerRegistrationContext(app, containerConfiguration, typeResolver);
            containerRegistrationContext.ConfigureContainerRoutes();

            var memberApiBuilders = app.Services.GetKeyedServices<IEntityMemberApiBuilder>(containerConfiguration.RoutePrefix);
            foreach (var memberBuilder in memberApiBuilders)
            {
                memberBuilder.Build(containerRegistrationContext);
            }
        }

        return app;
    }
}
