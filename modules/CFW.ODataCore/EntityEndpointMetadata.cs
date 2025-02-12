//using CFW.EntityApi.Models.Builders;
//using Microsoft.AspNetCore.OData;
//using Microsoft.EntityFrameworkCore;
//using Microsoft.EntityFrameworkCore.Metadata;

//namespace CFW.EntityApi;

//public class EntityEndpointMetadata<TDbContext, TEntity, TKey>
//    : IEntityQuerySetting<TEntity, TKey>
//    where TDbContext : DbContext
//    where TEntity : class
//{
//    private readonly DbContextEntitiesRegistrator<TDbContext> _dbEntitiesBuilder;
//    private readonly IEntityType _entityType;
//    private readonly IProperty _primaryKeyProperty;
//    private readonly ODataOptions _globalODataOptions;
//    private readonly IHost _host;

//    public EntityEndpointMetadata(IEntityType entityType
//        , IProperty primaryKeyProperty
//        , DbContextEntitiesRegistrator<TDbContext> dbEntitiesBuilder
//        , IHost host
//        , ODataOptions globalODataOptions)
//    {
//        _dbEntitiesBuilder = dbEntitiesBuilder;
//        _entityType = entityType;
//        _primaryKeyProperty = primaryKeyProperty;
//        _globalODataOptions = globalODataOptions;
//        _host = host;

//    }

//    public RouteGroupBuilder EntityRouteGroup { get; private set; } = default!;

//    public void Register(ContainerConfiguration containerRegistrator)
//    {
//        var entityConfig = typeof(EntityEndpointMetadata<TDbContext, TEntity, TKey>);
//        var customConfigType = containerRegistrator.CacheTypes!
//            .SingleOrDefault(x => x.BaseType == entityConfig);

//        if (customConfigType is not null)
//        {
//            var endpointSetting = (EntityEndpointMetadata<TDbContext, TEntity, TKey>)ActivatorUtilities
//                .CreateInstance(_host.Services, customConfigType);

//            endpointSetting.Register(containerRegistrator);
//            return;
//        }

//        var routeNameFormater = _dbEntitiesBuilder.RouteNameFormatter;
//        var routeName = routeNameFormater(_entityType);
//        var odataServiceProvider = _globalODataOptions.RouteComponents[containerRegistrator.RoutePrefix].ServiceProvider;
//        var containerGroupRoute = containerRegistrator.ContainerRegistrationContext.RouteGroupBuilder;

//        var entityRouteGroup = containerGroupRoute.MapGroup(routeName);

//        //default register
//        EntityRouteGroup = containerGroupRoute
//            .MapGroup(routeName)
//            .WithTags(routeName);
//    }
//}
