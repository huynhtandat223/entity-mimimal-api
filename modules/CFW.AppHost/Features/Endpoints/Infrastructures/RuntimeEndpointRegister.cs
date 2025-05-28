using CFW.AppHost.Features.Shared;
using CFW.Core.Dependencies;
using CFW.DynamicApi;
using CFW.DynamicApi.Interceptors.OData;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;
using System.Reflection;

namespace CFW.AppHost.Features.Endpoints.Infrastructures;

public class RuntimeEndpointRegister : ITransientService
{
    private static readonly Dictionary<Type, Func<HttpContext, Task<object?>>> _getQueryableMethodCache = new();

    private readonly AppDbContext _db;
    private readonly RuntimeTypeRegistry _runtimeTypeRegistry;

    public RuntimeEndpointRegister(AppDbContext db, RuntimeTypeRegistry runtimeTypeRegistry)
    {
        _db = db;
        _runtimeTypeRegistry = runtimeTypeRegistry;
    }

    public async Task ResiterEndpoints(IEndpointRouteBuilder routeBuilder)
    {
        var containerConfigurations = await _db
            .Set<Models.ContainerConfiguration>()
            .Where(x => x.Endpoints!.Any())
            .Include(x => x.Endpoints)!.ThenInclude(x => x.RuntimeEntityDefinition)
            .ToListAsync();

        var runtimeEntityDefinitions = containerConfigurations
            .SelectMany(x => x.Endpoints!.Select(e => e.RuntimeEntityDefinition))
            .Where(x => x is not null)
            .Distinct()
            .ToList();
        var loadedTypes = await _runtimeTypeRegistry.LoadRuntimeTypes(runtimeEntityDefinitions!);

        foreach (var containerConfig in containerConfigurations)
        {
            var containerGroupBuilder = routeBuilder.MapGroup(containerConfig.RoutePrefix);
            var methods = containerConfig.Endpoints!
                .Select(x => x.Method.ToString())
                .Distinct();

            containerGroupBuilder.MapMethods("/{path}", methods, async (string path, HttpContext httpContext) =>
            {
                var method = httpContext.Request.Method;
                var endpoint = containerConfig.Endpoints!.FirstOrDefault(x => x.Path == $"/{path}"
                    && x.Method.ToString() == method);

                if (endpoint is null)
                    return null;

                var typeDef = endpoint.RuntimeEntityDefinition;
                if (typeDef is null)
                    throw new NotImplementedException();

                var fullTypeName = string.Join('.', typeDef.Namespace, typeDef.Name);
                var loadedType = loadedTypes.FirstOrDefault(x => x.FullName == fullTypeName);
                if (loadedType is null)
                    throw new NotImplementedException();

                var result = await CallGetQueryableAsync(loadedType!, httpContext);

                return result;
            });
        }
    }

    public static Task<object?> CallGetQueryableAsync(Type entityType, HttpContext httpContext)
    {
        if (!_getQueryableMethodCache.TryGetValue(entityType, out var func))
        {
            var method = typeof(RuntimeEndpointRegister)
                .GetMethod(nameof(GetQueryable), BindingFlags.NonPublic | BindingFlags.Static)!
                .MakeGenericMethod(entityType);

            var contextParam = Expression.Parameter(typeof(HttpContext), "httpContext");

            var call = Expression.Call(null, method, contextParam);

            var lambda = Expression.Lambda<Func<HttpContext, Task<object?>>>(call, contextParam);
            func = lambda.Compile();

            _getQueryableMethodCache[entityType] = func;
        }

        return func(httpContext);
    }

    private static async Task<object?> GetQueryable<T>(HttpContext httpContext)
        where T : class
    {
        var serviceProvider = httpContext.RequestServices;
        var interceptor = serviceProvider.GetRequiredService<ODataFeatureInterceptor<T>>();
        var db = serviceProvider.GetRequiredService<RuntimeDbContext>();

        var queryable = db.Set<T>().AsNoTracking();

        var properties = typeof(T).GetProperties()
            .Select(x => new PropertyMetadata
            {
                ClrType = x.PropertyType
            ,
                IsRequired = true
            ,
                Name = x.Name
            ,
                PropertyType = PropertyType.Scalar
            ,
                IsKey = x.Name == "Id" ? true : false
            });

        var result = await interceptor.OnExecutedAsync(httpContext, new DynamicApiOperation
        {
            AllowedProperties = properties
        }, queryable);

        return result;
    }
}
