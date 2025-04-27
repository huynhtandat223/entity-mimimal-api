using Humanizer;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System.Linq.Expressions;
using System.Reflection;

namespace CFW.DynamicApi.Buiders;

public class DynamicEntityGroupBuilder
{
    public required string RouteName { get; set; }

    protected Action<RouteGroupBuilder>? _routeConfigurator;
    protected readonly List<DynamicApiOperation> _operations = new();
    protected readonly List<string> _includeProperties = new();
    protected readonly List<string> _excludeProperties = new();
    protected HttpMethod _httpMethod = HttpMethod.Get;

    public IReadOnlyCollection<DynamicApiOperation> Operations => _operations.AsReadOnly();

    internal virtual IEnumerable<DynamicApiOperation> Build()
    {
        foreach (var op in _operations)
        {
            var originalConfigure = op.ConfigureRoute;
            op.ConfigureRoute = group =>
            {
                _routeConfigurator?.Invoke(group);
                originalConfigure?.Invoke(group);
            };

            op.Route = $"/{RouteName}{op.Route}";
        }

        return _operations;
    }

    public static DynamicEntityGroupBuilder<TEntity, TDbContext> Create<TEntity, TDbContext>()
        where TEntity : class
        where TDbContext : DbContext
    {
        return new DynamicEntityGroupBuilder<TEntity, TDbContext>
        {
            RouteName = typeof(TEntity).Name.Pluralize().ToLowerInvariant()
        };
    }
}

public class DynamicEntityGroupBuilder<TEntity, TDbContext> : DynamicEntityGroupBuilder<TEntity>
    where TEntity : class
    where TDbContext : DbContext
{
    public DynamicEntityGroupBuilder<TEntity> AddQueryApi(Action<DynamicApiOperation>? operationConfig = null)
    {
        var result = new DynamicApiOperation
        {
            HttpMethod = HttpMethod.Get.Method,
            Handler = async (context) =>
            {
                var db = context.RequestServices.GetRequiredService<TDbContext>();
                var entities = db.Set<TEntity>().AsNoTracking();
                return await Task.FromResult(entities);
            }
        };

        operationConfig?.Invoke(result);

        WithOperation(result);
        return this;
    }

    //public DynamicEntityGroupBuilder<TEntity> AddCreationApi(Action<DynamicApiOperation>? operationConfig = null)
    //{
    //    var result = new DynamicApiOperation
    //    {
    //        HttpMethod = HttpMethod.Post.Method,
    //        Handler = async (EntityDelta<TEntity> delta) =>
    //        {
    //            var db = context.RequestServices.GetRequiredService<TDbContext>();
    //            var entities = db.Set<TEntity>().AsNoTracking();
    //            return await Task.FromResult(entities);
    //        }
    //    };

    //    operationConfig?.Invoke(result);

    //    WithOperation(result);
    //    return this;
    //}
}

public class DynamicEntityGroupBuilder<TEntity> : DynamicEntityGroupBuilder where TEntity : class
{
    protected DynamicEntityGroupBuilder()
    {
        RouteName = typeof(TEntity).Name.Pluralize().ToLowerInvariant();
    }

    public DynamicEntityGroupBuilder<TEntity> WithRouteName(string routeName)
    {
        RouteName = routeName;
        return this;
    }

    public DynamicEntityGroupBuilder<TEntity> WithOperation(DynamicApiOperation apiOperation)
    {
        _operations.Add(apiOperation);
        return this;
    }

    public DynamicEntityGroupBuilder<TEntity> WithHttpMethod(HttpMethod httpMethod)
    {
        _httpMethod = httpMethod;
        return this;
    }

    public DynamicEntityGroupBuilder<TEntity> ConfigureRouteGroup(Action<RouteGroupBuilder> configure)
    {
        _routeConfigurator = configure;
        return this;
    }

    public DynamicEntityGroupBuilder<TEntity> IncludeProperties(params Expression<Func<TEntity, object>>[] properties)
    {
        foreach (var prop in properties)
        {
            var name = GetPropertyName(prop);
            _includeProperties.Add(name);
        }
        return this;
    }

    public DynamicEntityGroupBuilder<TEntity> ExcludeProperties(params Expression<Func<TEntity, object>>[] properties)
    {
        foreach (var prop in properties)
        {
            var name = GetPropertyName(prop);
            _excludeProperties.Add(name);
        }
        return this;
    }

    protected string GetPropertyName(Expression<Func<TEntity, object>> expression)
    {
        if (expression.Body is MemberExpression member)
            return member.Member.Name;
        if (expression.Body is UnaryExpression unary && unary.Operand is MemberExpression memberOperand)
            return memberOperand.Member.Name;
        throw new InvalidOperationException("Invalid expression");
    }

    public IReadOnlyList<string> ResolveSelectedProperties()
    {
        if (_includeProperties.Count == 0)
        {
            var props = typeof(TEntity).GetProperties(BindingFlags.Instance | BindingFlags.Public);
            return props.Select(p => p.Name).Except(_excludeProperties).ToList();
        }
        else
        {
            return _includeProperties.Except(_excludeProperties).ToList();
        }
    }
}
