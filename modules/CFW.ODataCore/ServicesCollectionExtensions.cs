using CFW.ODataCore.Attributes;
using CFW.ODataCore.DefaultHandlers;
using CFW.ODataCore.Intefaces;
using CFW.ODataCore.Models;
using CFW.ODataCore.Models.Metadata;
using CFW.ODataCore.RouteMappers;
using Microsoft.AspNetCore.OData;
using Microsoft.AspNetCore.OData.Formatter;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using Microsoft.OData;
using Microsoft.OData.ModelBuilder;
using System.Reflection;
using System.Text;

namespace CFW.ODataCore;

public record EntityKey(string RoutePrefix, string EntityName);

public static class ServicesCollectionExtensions
{
    /// <summary>
    /// Don't allow to call this method multiple times yet
    /// </summary>
    /// <param name="services"></param>
    /// <param name="setupAction"></param>
    /// <param name="defaultRoutePrefix"></param>
    /// <returns></returns>
    public static IServiceCollection AddEntityMinimalApi(this IServiceCollection services
        , Action<EntityMimimalApiOptions>? setupAction = null
        , string defaultRoutePrefix = Constants.DefaultODataRoutePrefix)
    {
        var coreOptions = new EntityMimimalApiOptions { DefaultRoutePrefix = defaultRoutePrefix };
        if (setupAction is not null)
            setupAction(coreOptions);

        //TODO: refactor ODataOptions can use multiple route prefix
        services.AddOptions<ODataOptions>().Configure(coreOptions.ODataOptions);
        services.AddSingleton(coreOptions);

        //register OData output formatter
        var formatter = new ODataOutputFormatter([ODataPayloadKind.ResourceSet]);
        formatter.SupportedEncodings.Add(Encoding.UTF8);
        services.TryAddSingleton(formatter);

        services.TryAddScoped(typeof(IEntityCreationHandler<>), typeof(EntityCreationHandler<>));
        services.TryAddScoped(typeof(IEntityPatchHandler<,>), typeof(EntityPatchHandler<,>));
        services.TryAddScoped(typeof(IEntityDeletionHandler<,>), typeof(EntityDeletionHandler<,>));

        //register default db context provider
        if (coreOptions.DefaultDbContext is not null)
        {
            var contextProvider = typeof(ContextProvider<>).MakeGenericType(coreOptions.DefaultDbContext);
            services.AddKeyedScoped(typeof(IDbContextProvider), coreOptions.DefaultRoutePrefix, contextProvider);
        }

        var entityConfigInfoes = coreOptions.MetadataContainerFactory.CacheType
            .Where(x => x.GetCustomAttributes<EntityAttribute>().Any())
            .Where(x => x.BaseType is not null
                && x.BaseType.IsGenericType
                && x.BaseType.GetGenericTypeDefinition() == typeof(EntityEndpoint<>))
            .Select(x => new
            {
                TargetType = x,
                Attributes = x.GetCustomAttributes<EntityAttribute>()
                .Where(a => a.RoutePrefix == coreOptions.DefaultRoutePrefix).ToList()
            })
            .Where(x => x.Attributes.Any())
            .ToList();

        foreach (var entityConfigInfo in entityConfigInfoes)
        {
            var baseEndpointConfigType = entityConfigInfo.TargetType.BaseType!;

            var attribute = entityConfigInfo.Attributes.Single();

            var entityKey = new EntityKey(attribute.RoutePrefix, attribute.Name);
            services.TryAddKeyedSingleton(baseEndpointConfigType, entityKey, entityConfigInfo.TargetType);
        }

        return services;
    }

    public static void UseEntityMinimalApi(this WebApplication app)
    {
        var minimalApiOptionsList = app.Services.GetServices<EntityMimimalApiOptions>();

        foreach (var minimalApiOptions in minimalApiOptionsList)
        {

            //resolve all metadata containers
            var sanitizedRoutePrefix = StringUtils.SanitizeRoute(minimalApiOptions.DefaultRoutePrefix);
            var containerFactory = minimalApiOptions.MetadataContainerFactory;
            var container = containerFactory
                .CreateMetadataContainer(minimalApiOptions);

            using var scope = app.Services.CreateScope();
            var dbProvider = scope.ServiceProvider.GetRequiredKeyedService<IDbContextProvider>(minimalApiOptions.DefaultRoutePrefix);
            var db = dbProvider.GetDbContext();

            var odataOptions = app.Services.GetRequiredService<IOptions<ODataOptions>>().Value;
            var defaultModel = new ODataConventionModelBuilder().GetEdmModel();


            if (minimalApiOptions.DbContextOptions is not null
                && minimalApiOptions.DbContextOptions.AutoGenerateEndpoints is not null)
            {
                var autoGenerationOptions = minimalApiOptions.DbContextOptions.AutoGenerateEndpoints;

                var routePrefix = autoGenerationOptions.RoutePrefix ?? minimalApiOptions.DefaultRoutePrefix;
                var sanitizedAutoRoutePrefix = StringUtils.SanitizeRoute(routePrefix);
                var routeNameFormater = autoGenerationOptions.RouteNameFormatter;

                var defaultQueryOptions = minimalApiOptions.DbContextOptions.AutoGenerateEndpoints.QueryOptions;
                var nestedLevel = minimalApiOptions.DbContextOptions.AutoGenerateEndpoints.NestedLevel;


                var buildedClrTypes = container.MetadataEntities.Select(y => y.SourceType).ToList();
                var entityTypes = db.Model.GetEntityTypes()
                    .Where(x => x.FindPrimaryKey() is not null
                        && x.FindPrimaryKey()!.Properties.Count == 1) //only support single key entity
                    .Where(x => x.ClrType is not null && !buildedClrTypes.Contains(x.ClrType))
                    .ToList();

                foreach (var entityType in entityTypes)
                {
                    var metadata = new MetadataEntity
                    {
                        Container = container,
                        SourceType = entityType.ClrType!,
                        Name = routeNameFormater(entityType),
                        Methods = [ApiMethod.Query, ApiMethod.GetByKey, ApiMethod.Patch, ApiMethod.Delete, ApiMethod.Post],
                        EFCoreEntityType = entityType,
                        HandlerAttributes = Array.Empty<EntityHandlerAttribute>(),
                        ODataQueryOptions = defaultQueryOptions!,
                        NestedLevel = nestedLevel,
                    };
                    container.MetadataEntities.Add(metadata);
                }
            }

            var containerGroupRoute = app.MapGroup(container.RoutePrefix);
            container.Options.ConfigureContainerRouteGroup?.Invoke(containerGroupRoute);

            var metadataEntities = container.MetadataEntities;
            foreach (var metadataEntity in metadataEntities)
            {
                var entityEndpointType = typeof(EntityEndpoint<>).MakeGenericType(metadataEntity.SourceType);
                var endtityEndpointObj = app.Services
                    .GetKeyedServices(entityEndpointType, metadataEntity.Name)
                    .FirstOrDefault() ?? Activator.CreateInstance(entityEndpointType)!;

                var entityEndpoint = endtityEndpointObj as EntityEndpoint;
                metadataEntity.ODataQueryOptions.SetIgnoreQueryOptions(odataOptions.QueryConfigurations, entityEndpoint!);
                RegisterEntityComponents(app, metadataEntity, containerGroupRoute);
            }

            var unboundOperations = container.UnboundOperations;
            foreach (var unboundOperation in unboundOperations)
            {
                unboundOperation.AddServices(containerGroupRoute);
            }

            //Add internal odata service providers
            odataOptions.AddRouteComponents(container.RoutePrefix, defaultModel);
            container.ODataInternalServiceProvider = odataOptions.RouteComponents[container.RoutePrefix].ServiceProvider;

            minimalApiOptions.MetadataContainer = container;
        }

    }

    private static void RegisterEntityComponents(WebApplication app
        , MetadataEntity metadataEntity, RouteGroupBuilder containerGroupRoute)
    {
        var sourceType = metadataEntity.SourceType;
        var allowMethods = metadataEntity.Methods;
        var serviceProvider = app.Services;

        var entityRoute = containerGroupRoute
            .MapGroup(metadataEntity.Name)
            .WithTags(metadataEntity.Name)
            .WithMetadata(metadataEntity);

        //register entity authorization data
        //TODO: move to entity metadata
        var authorizeAttributes = sourceType.GetCustomAttributes<EntityAuthorizeAttribute>();
        var authorizeDataOfMethods = authorizeAttributes.SelectMany(x => x.ApplyMethods
        .Select(m => new { Method = m, Attribute = x })
        .Where(x => allowMethods.Contains(x.Method))
        .GroupBy(x => x.Method));
        foreach (var authorizeDataOfMethod in authorizeDataOfMethods)
        {
            if (authorizeDataOfMethod.Count() > 1)
                throw new InvalidOperationException($"Duplicate method {authorizeDataOfMethod.Key} found in {sourceType}");

            var authorizeData = authorizeDataOfMethod.Single().Attribute;
            entityRoute = entityRoute.RequireAuthorization([authorizeData]);
        }

        //register CRUD routes
        foreach (var method in metadataEntity.Methods)
        {
            if (method == ApiMethod.Query)
            {
                var routeMapperType = typeof(EntityQueryRouteMapper<>)
                    .MakeGenericType(metadataEntity.SourceType);
                var routeMapper = (IRouteMapper)ActivatorUtilities
                    .CreateInstance(serviceProvider, routeMapperType, metadataEntity);

                routeMapper.MapRoutes(entityRoute);
            }

            if (method == ApiMethod.GetByKey)
            {
                var routeMapperType = typeof(EntityGetByKeyRouteMapper<>)
                    .MakeGenericType(metadataEntity.SourceType);
                var routeMapper = (IRouteMapper)ActivatorUtilities
                    .CreateInstance(serviceProvider, routeMapperType, metadataEntity);

                routeMapper.MapRoutes(entityRoute);
            }

            if (method == ApiMethod.Patch)
            {
                var routeMapperType = typeof(EntityPatchRouteMapper<>)
                    .MakeGenericType(metadataEntity.SourceType);
                var routeMapper = (IRouteMapper)ActivatorUtilities
                    .CreateInstance(serviceProvider, routeMapperType, metadataEntity);

                routeMapper.MapRoutes(entityRoute);
            }

            if (method == ApiMethod.Delete)
            {
                var routeMapperType = typeof(EntityDeleteRouteMapper<>)
                    .MakeGenericType(metadataEntity.SourceType);
                var routeMapper = (IRouteMapper)ActivatorUtilities
                    .CreateInstance(serviceProvider, routeMapperType, metadataEntity);

                routeMapper.MapRoutes(entityRoute);
            }

            if (method == ApiMethod.Post)
            {
                var routeMapperType = typeof(EntityCreationRouteMapper<>)
                    .MakeGenericType(metadataEntity.SourceType);
                var routeMapper = (IRouteMapper)ActivatorUtilities
                    .CreateInstance(serviceProvider, routeMapperType, metadataEntity);

                routeMapper.MapRoutes(entityRoute);
            }
        }

        //register entity operation routes
        foreach (var operation in metadataEntity.Operations)
        {
            operation.AddServices(entityRoute);
        }
    }
}
