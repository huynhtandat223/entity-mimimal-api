using CFW.AppHost.Features.Core;
using CFW.AppHost.Infrastructures.DbContextExtensions.Services.Runtimes;
using CFW.Core.Dependencies;
using CFW.DynamicApi;
using CFW.DynamicApi.Interceptors.OData;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;
using System.Reflection;
using HttpMethod = CFW.AppHost.Features.Endpoints.Models.HttpMethod;

namespace CFW.AppHost.Features.Endpoints.Services;

public record RouteDef
{
    public string RoutePrefix { get; set; } = string.Empty;

    public string Path { get; set; } = string.Empty;

    public HttpMethod HttpMethod { set; get; }

    public virtual bool Equals(RouteDef? other)
    {
        if (other is null) return false;
        return string.Equals(RoutePrefix, other.RoutePrefix, StringComparison.OrdinalIgnoreCase)
            && HttpMethod == other.HttpMethod
            && string.Equals(Path, other.Path, StringComparison.OrdinalIgnoreCase);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(
            StringComparer.OrdinalIgnoreCase.GetHashCode(RoutePrefix),
            HttpMethod.GetHashCode(),
            StringComparer.OrdinalIgnoreCase.GetHashCode(Path));
    }
}

public class RuntimeEndpointRegister : ITransientService
{
    private static readonly Dictionary<Type, Func<HttpContext, RuntimeTypeRegistry, Task<object?>>> _getQueryableMethodCache = new();

    private readonly AppRequestContext _appRequestContext;
    private readonly RuntimeTypeRegistry _runtimeTypeRegistry;

    public RuntimeEndpointRegister(RuntimeTypeRegistry runtimeTypeRegistry
        , AppRequestContext appRequestContext)
    {
        _runtimeTypeRegistry = runtimeTypeRegistry;
        _appRequestContext = appRequestContext;
    }

    public async Task ResiterEndpoints(IEndpointRouteBuilder routeBuilder)
    {
        var db = await _appRequestContext.GetOrCreateDbContext();
        if (db is null)
            return;

        var containerConfigurations = await db
            .Set<Models.ContainerConfiguration>()
            .Where(x => x.Endpoints!.Any())
            .Include(x => x.Endpoints)!.ThenInclude(x => x.RuntimeEntityDefinition).ThenInclude(x => x!.Properties)
            .ToListAsync();

        var runtimeEntityDefinitions = containerConfigurations
            .SelectMany(x => x.Endpoints!.Select(e => e.RuntimeEntityDefinition))
            .Where(x => x is not null)
            .Distinct()
            .ToList();
        var loadedTypes = await _runtimeTypeRegistry.LoadRuntimeTypes(runtimeEntityDefinitions!);

        var endpoints = containerConfigurations
            .SelectMany(x => x.Endpoints!)
            .GroupBy(x => new RouteDef { HttpMethod = x.Method, Path = x.Path, RoutePrefix = x.ContainerConfiguration.RoutePrefix }, x => x)
            .ToDictionary(x => x.Key, x => x.Single());

        var containerGroupBuilders = endpoints
            .Select(x => x.Key.RoutePrefix)
            .Distinct()
            .ToDictionary(x => x, x => routeBuilder.MapGroup(x), StringComparer.InvariantCultureIgnoreCase);

        foreach (var endpointInfo in endpoints)
        {
            var containerGroupBuilder = containerGroupBuilders[endpointInfo.Key.RoutePrefix];
            var methods = new[] { endpointInfo.Key.HttpMethod.ToString() };
            var endpoint = endpointInfo.Value;
            var path = $"/{endpoint.Path?.TrimStart('/')}";

            containerGroupBuilder.MapMethods(path, methods, async (HttpContext httpContext) =>
            {
                var typeDef = endpoint.RuntimeEntityDefinition;
                if (typeDef is null)
                    throw new NotImplementedException();

                var fullTypeName = string.Join('.', typeDef.Namespace, typeDef.Name);
                var loadedType = loadedTypes.FirstOrDefault(x => x.FullName == fullTypeName);
                if (loadedType is null)
                    throw new NotImplementedException();

                var result = await CallGetQueryableAsync(loadedType!, httpContext, _runtimeTypeRegistry);

                return result;
            }).WithMetadata(endpointInfo.Value);
        }
    }

    public static async Task RegisterDynamicRoute(string routePrefix, RouteGroupBuilder containerGroupBuilder)
    {
        await Task.CompletedTask;
        //var methods = new[] { HttpMethod.GET.ToString()
        //    , HttpMethod.POST.ToString()
        //    , HttpMethod.DELETE.ToString()
        //    , HttpMethod.PATCH.ToString() };

        //containerGroupBuilder.MapMethods("/{path}", methods, async (string path, HttpContext httpContext) =>
        //{
        //    var db = httpContext.RequestServices.GetRequiredService<AppDbContext>();
        //    var method = Enum.Parse<HttpMethod>(httpContext.Request.Method);

        //    var endpoint = await db
        //        .Set<Models.Endpoint>()
        //        .Include(x => x.RuntimeEntityDefinition)
        //        .FirstOrDefaultAsync(x => x.Path == path
        //            && x.Method == method
        //            && x.ContainerConfiguration.RoutePrefix == routePrefix);

        //    if (endpoint is null)
        //        return Results.NotFound();

        //    var typeDef = endpoint.RuntimeEntityDefinition;
        //    if (typeDef is null)
        //        throw new NotImplementedException();

        //    var result = await CallGetQueryableAsync(loadedType!, httpContext, _runtimeTypeRegistry);

        //    return result;
        //});
    }

    public static async Task<object?> CallGetQueryableAsync(Type entityType, HttpContext httpContext, RuntimeTypeRegistry runtimeTypeRegistry)
    {
        if (!_getQueryableMethodCache.TryGetValue(entityType, out var func))
        {
            var method = typeof(RuntimeEndpointRegister)
                .GetMethod(nameof(GetQueryable), BindingFlags.NonPublic | BindingFlags.Static)!
                .MakeGenericMethod(entityType);

            var contextParam = Expression.Parameter(typeof(HttpContext), "httpContext");
            var runtimeTypeRegistryExpr = Expression.Parameter(typeof(RuntimeTypeRegistry), "runtimeTypeRegistry");

            var call = Expression.Call(null, method, contextParam, runtimeTypeRegistryExpr);

            var lambda = Expression.Lambda<Func<HttpContext, RuntimeTypeRegistry, Task<object?>>>(call, contextParam, runtimeTypeRegistryExpr);
            func = lambda.Compile();

            _getQueryableMethodCache[entityType] = func;
        }

        return await func(httpContext, runtimeTypeRegistry);
    }

    private static async Task<object?> GetQueryable<T>(HttpContext httpContext, RuntimeTypeRegistry runtimeTypeRegistry)
        where T : class
    {
        var serviceProvider = httpContext.RequestServices;
        var interceptor = serviceProvider.GetRequiredService<ODataFeatureInterceptor<T>>();

        var options = new DbContextOptionsBuilder<RuntimeDbContext<T>>()
            .UseSqlServer("Server=localhost\\SQLEXPRESS;Database=db;User ID=sa;Password=123456;Encrypt=True;TrustServerCertificate=True;MultipleActiveResultSets=True;")
            .Options;
        var db = new RuntimeDbContext<T>(options);

        var queryable = db.Set<T>().AsNoTracking();

        var properties = typeof(T).GetProperties()
            .Select(x => new PropertyMetadata
            {
                ClrType = x.PropertyType,
                IsRequired = true,
                Name = x.Name,
                PropertyType = PropertyType.Scalar,
                IsKey = x.Name == "Id" ? true : false
            });

        var result = await interceptor.OnExecutedAsync(httpContext, new DynamicApiOperation
        {
            AllowedProperties = properties
        }, queryable);

        return result;
    }
}
