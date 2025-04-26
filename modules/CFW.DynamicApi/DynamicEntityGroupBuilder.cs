using Humanizer;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using System.Linq.Expressions;
using System.Reflection;

namespace CFW.DynamicApi;

public class DynamicEntityGroupBuilder<TEntity> where TEntity : class
{
    protected string? _routeName;
    protected Action<RouteGroupBuilder>? _routeConfigurator;
    protected readonly List<DynamicApiOperation> _operations = new();
    protected readonly List<string> _includeProperties = new();
    protected readonly List<string> _excludeProperties = new();

    protected DynamicEntityGroupBuilder() { }

    public static DynamicEntityGroupBuilder<TEntity> Create() => new();

    public DynamicEntityGroupBuilder<TEntity> WithRouteName(string routeName)
    {
        _routeName = routeName;
        return this;
    }

    public DynamicEntityGroupBuilder<TEntity> ConfigureRouteGroup(Action<RouteGroupBuilder> configure)
    {
        _routeConfigurator = configure;
        return this;
    }

    public virtual DynamicEntityGroupBuilder<TEntity> AllowListing(Action<DynamicOperationBuilder>? configure = null)
    {
        var builder = new DynamicOperationBuilder()
            .WithMethod(HttpMethods.Get)
            .WithRoute("/");

        configure?.Invoke(builder);

        _operations.Add(builder.Build());
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

    internal virtual IEnumerable<DynamicApiOperation> Build()
    {
        var routeName = _routeName ?? typeof(TEntity).Name.Pluralize().ToLowerInvariant();

        foreach (var op in _operations)
        {
            var originalConfigure = op.ConfigureRoute;
            op.ConfigureRoute = group =>
            {
                group.WithTags(typeof(TEntity).Name.Pluralize());
                _routeConfigurator?.Invoke(group);
                originalConfigure?.Invoke(group);
            };

            op.Route = $"/{routeName}{op.Route}";
        }

        if (_operations.Count == 0)
        {
            var op = new DynamicApiOperation
            {
                HttpMethod = HttpMethods.Get,
                Route = $"/{routeName}",
                Handler = async ctx => $"Queried {typeof(TEntity).Name} list",
                ConfigureRoute = group =>
                {
                    group.WithTags(typeof(TEntity).Name.Pluralize());
                    _routeConfigurator?.Invoke(group);
                }
            };

            return new[] { op };
        }

        return _operations;
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
