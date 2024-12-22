﻿using CFW.ODataCore.EFCore;
using CFW.ODataCore.Features.BoundActions;
using CFW.ODataCore.Features.Core;
using CFW.ODataCore.Features.EntitySets;
using CFW.ODataCore.Features.EntitySets.Handlers;
using CFW.ODataCore.Features.Shared;
using CFW.ODataCore.Features.UnBoundActions;
using CFW.ODataCore.Features.UnboundFunctions;
using Microsoft.AspNetCore.OData;
using Microsoft.EntityFrameworkCore;

namespace CFW.ODataCore.Extensions;

public static class ServicesCollectionExtensions
{
    public static IServiceCollection AddGenericDbContext<TDbContext>(this IServiceCollection services
        , ServiceLifetime dbServiceLifetime = ServiceLifetime.Scoped)
        where TDbContext : DbContext
    {
        var contextProvider = typeof(ContextProvider<>).MakeGenericType(typeof(TDbContext));
        services.Add(new ServiceDescriptor(typeof(IODataDbContextProvider), contextProvider, dbServiceLifetime));

        return services;
    }

    public static IMvcBuilder AddGenericODataEndpoints(this IMvcBuilder mvcBuilder, BaseODataMetadataResolver? oDataTypeResolver = null)
    {
        oDataTypeResolver ??= new DefaultODataMetadataResolver(Constants.DefaultODataRoutePrefix);
        var containers = oDataTypeResolver.CreateContainers();

        var services = mvcBuilder.Services;
        services.AddScoped(typeof(IRequestHandler<,>), typeof(DefaultRequestHandler<,>));
        services.AddScoped(typeof(IQueryHandler<,>), typeof(DefaultQueryHandler<,>));
        services.AddScoped(typeof(IGetByKeyHandler<,>), typeof(DefaultGetByKeyHandler<,>));
        services.AddScoped(typeof(ICreateHandler<,>), typeof(DefaultCreateHandler<,>));
        services.AddScoped(typeof(IDeleteHandler<,>), typeof(DefaultDeleteHandler<,>));
        services.AddScoped(typeof(IPatchHandler<,>), typeof(DefaultPatchHandler<,>));

        services.AddScoped(typeof(IBoundActionRequestHandler<,,,>), typeof(DefaultBoundActionRequestHandler<,,,>));
        services.AddScoped(typeof(IBoundOperationRequestHandler<,,,>), typeof(DefaultBoundOperationRequestHandler<,,,>));

        services.AddScoped(typeof(IUnboundActionRequestHandler<,>), typeof(DefaultActionRequestHandler<,>));
        services.AddScoped(typeof(IUnboundFunctionRequestHandler<,>), typeof(DefaultActionRequestHandler<,>));

        foreach (var container in containers)
        {
            var boundActionMetadata = container.EntityMetadataList
                .SelectMany(x => x.BoundActionMetadataList)
                .ToList();
            foreach (var metadata in boundActionMetadata)
            {
                var serviceType = metadata.ResponseType == typeof(Result)
                    ? typeof(IODataActionHandler<>).MakeGenericType(metadata.RequestType)
                    : typeof(IODataActionHandler<,>).MakeGenericType(metadata.RequestType, metadata.ResponseType);

                services.AddScoped(serviceType, metadata.HandlerType);
            }

            foreach (var metadata in container.EntityMetadataList
                .SelectMany(x => x.BoundFunctionMetadataList))
            {
                var serviceType = metadata.ResponseType == typeof(Result)
                    ? typeof(IODataActionHandler<>).MakeGenericType(metadata.RequestType)
                    : typeof(IODataActionHandler<,>).MakeGenericType(metadata.RequestType, metadata.ResponseType);
                services.AddScoped(serviceType, metadata.HandlerType);
            }


            foreach (var metadata in container.UnBoundActions)
            {
                var serviceType = metadata.ResponseType == typeof(Result)
                    ? typeof(IODataActionHandler<>).MakeGenericType(metadata.RequestType)
                    : typeof(IODataActionHandler<,>).MakeGenericType(metadata.RequestType, metadata.ResponseType);

                services.AddScoped(serviceType, metadata.HandlerType);
            }

            foreach (var metadata in container.UnboundFunctions)
            {
                var serviceType = metadata.ResponseType == typeof(Result)
                    ? typeof(IODataActionHandler<>).MakeGenericType(metadata.RequestType)
                    : typeof(IODataActionHandler<,>).MakeGenericType(metadata.RequestType, metadata.ResponseType);

                services.AddScoped(serviceType, metadata.HandlerType);
            }
        }

        services.AddSingleton(new GenericODataConfig { IsEnabled = true });
        return Build(containers, mvcBuilder);
    }

    public static IMvcBuilder Build(IEnumerable<ODataMetadataContainer> containers, IMvcBuilder mvcBuilder)
    {
        var entitySetControllerTypes = containers
            .SelectMany(x => x.EntityMetadataList.Select(x => x.ControllerType))
            .ToList();
        mvcBuilder = mvcBuilder.AddMvcOptions(options =>
        {
            foreach (var container in containers)
            {
                options.Conventions.Add(new EntitySetsConvention(container));

                var boundActionMetadata = container.EntityMetadataList
                    .SelectMany(x => x.BoundActionMetadataList)
                    .ToList();

                if (boundActionMetadata.Any())
                    options.Conventions.Add(new BoundActionsConvention(boundActionMetadata));

                var hasBoundFunctions = container.EntityMetadataList
                    .SelectMany(x => x.BoundFunctionMetadataList)
                    .Any();
                if (hasBoundFunctions)
                    options.Conventions.Add(new BoundOperationsConvention(container));

                if (container.UnBoundActions.Any())
                    options.Conventions.Add(new UnBoundActionsConvention(container));

                if (container.UnboundFunctions.Any())
                    options.Conventions.Add(new UnboundFunctionsConvention(container));
            }
        });

        return mvcBuilder.AddOData(options =>
        {
            options.EnableQueryFeatures();

            foreach (var metadataContainer in containers)
            {
                options.AddRouteComponents(
                    routePrefix: metadataContainer.RoutePrefix
                    , model: metadataContainer.EdmModel);
            }
        }).ConfigureApplicationPartManager(pm =>
        {
            foreach (var metadataContainer in containers)
            {
                pm.ApplicationParts.Add(metadataContainer);
            }
        });
    }

    public static void UseGenericODataEndpoints(this WebApplication app)
    {
        var odataConfig = app.Services.GetService<GenericODataConfig>();
        if (odataConfig is null || !odataConfig.IsEnabled)
            return;

        app.UseRouting();
        app.MapControllers();
    }
}
