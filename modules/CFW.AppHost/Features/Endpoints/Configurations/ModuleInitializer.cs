using CFW.AppHost.Features.Shared;
using CFW.Core.Dependencies;
using CFW.DynamicApi;
using CFW.DynamicApi.Interceptors.OData;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using System.Linq.Expressions;
using System.Reflection;

namespace CFW.AppHost.Features.Endpoints.Configurations;

public class ModuleInitializer : IModuleInitializer
{
    private readonly AppDbContext _db;
    private readonly RuntimeAsmConfig _runtimeAsmConfig;

    private static readonly Dictionary<Type, Func<HttpContext, Task<object?>>> _getQueryableMethodCache = new();


    public ModuleInitializer(AppDbContext db, IOptions<RuntimeAsmConfig> options)
    {
        _db = db;
        _runtimeAsmConfig = options.Value;
    }

    public async Task InitModule(IHostApplicationBuilder builder)
    {
        var entityDefs = await _db.Set<Models.Endpoint>()
            .Select(x => x.RuntimeEntityDefinition)
            .ToListAsync();

        var runtimeDir = _runtimeAsmConfig.GetRuntimeEntitiesDirOrDefault();
        var runtimeTypes = new List<Type>();

        foreach (var file in Directory.GetFiles(runtimeDir, "*.dll"))
        {
            try
            {
                // Load without locking the file
                var assemblyBytes = File.ReadAllBytes(file);
                var assembly = Assembly.Load(assemblyBytes);

                foreach (var type in assembly.GetTypes())
                {
                    // Optional: only add entity-related types
                    if (entityDefs.Any(e => type.FullName == $"{e.Namespace}.{e.Name}"))
                        runtimeTypes.Add(type);
                }
            }
            catch (Exception ex)
            {
                // Log and continue
                Console.WriteLine($"Failed to load assembly {file}: {ex.Message}");
            }
        }

        builder.Services.AddTransient(runtimeTypes);
    }



    public async Task RunModule(IHost app)
    {
        if (app is not IEndpointRouteBuilder endpointRouteBuilder)
            throw new InvalidOperationException();

        var containerConfigurations = await _db
            .Set<Models.ContainerConfiguration>()
            .Where(x => x.Endpoints!.Any())
            .Include(x => x.Endpoints)!.ThenInclude(x => x.RuntimeEntityDefinition)
            .ToListAsync();

        foreach (var containerConfig in containerConfigurations)
        {
            var containerGroupBuilder = endpointRouteBuilder.MapGroup(containerConfig.RoutePrefix);
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
                var fullPath = Path.Combine(_runtimeAsmConfig.GetRuntimeEntitiesDirOrDefault(), fullTypeName + ".dll");
                var assemblyBytes = File.ReadAllBytes(fullPath);
                var loadedAssembly = Assembly.Load(assemblyBytes);
                var loadedType = loadedAssembly.GetType(fullTypeName);

                var result = await CallGetQueryableAsync(loadedType!, httpContext);

                return result;
            });
        }
    }

    public static Task<object?> CallGetQueryableAsync(Type entityType, HttpContext httpContext)
    {
        if (!_getQueryableMethodCache.TryGetValue(entityType, out var func))
        {
            var method = typeof(ModuleInitializer)
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
        var db = serviceProvider.GetRequiredService<AppDbContext>();

        var queryable = db.Set<T>().AsNoTracking();

        var result = await interceptor.OnExecutedAsync(httpContext, new DynamicApiOperation
        {
            AllowedProperties = new List<PropertyMetadata>()
        }, queryable);

        return result;
    }
}
