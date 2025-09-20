using CFW.Core.Utils;
using Humanizer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Routing;
using System.Linq.Expressions;
using System.Text.Json.Serialization;

namespace CFW.DynamicApi.Buiders;

[Obsolete("Is need to refactor or simplify ????")]
public class DynamicEntityGroupBuilder
{
    public required string RouteName { get; set; }

    protected Action<RouteGroupBuilder>? _routeConfigurator;
    protected readonly List<DynamicApiOperation> _operations = new();
    protected readonly List<string> _includeProperties = new();
    protected readonly List<string> _excludeProperties = new();
    protected HttpMethod _httpMethod = HttpMethod.Get;

    public bool IsAllowAnonymous { private set; get; } = false;

    internal IList<IAuthorizeData> AuthorizeDatas => new List<IAuthorizeData>();

    protected virtual IEnumerable<PropertyMetadata> ResolveSelectedProperties() => Enumerable.Empty<PropertyMetadata>();

    public virtual JsonConverterFactory CreateJsonConverterFactory(IServiceProvider serviceProvider) { throw new NotImplementedException(); }

    public List<DynamicApiOperation> Operations => _operations;

    internal virtual IEnumerable<DynamicApiOperation> Build()
    {
        var properties = ResolveSelectedProperties();
        Properties = properties.ToList();
        foreach (var op in _operations)
        {
            var includeProperties = op._includeProperties.Any()
                ? properties.Where(x => op._includeProperties.Any(i => i == x.Name)).ToList()
                : properties;

            includeProperties = includeProperties
                .Where(x => !op._excludeProperties.Any(i => i == x.Name)).ToList();

            op.AllowedProperties = includeProperties;
            var originalConfigure = op.ConfigureRoute;
            op.ConfigureRoute = group =>
            {
                _routeConfigurator?.Invoke(group);
                originalConfigure?.Invoke(group);
            };
        }

        return _operations;
    }

    public IEnumerable<PropertyMetadata>? Properties { set; get; }
}

[Obsolete("Is need to refactor or simplify ????")]
public class DynamicEntityGroupBuilder<TEntity> : DynamicEntityGroupBuilder where TEntity : class
{
    protected DynamicEntityGroupBuilder()
    {
        RouteName = typeof(TEntity).Name.Pluralize().ToLowerInvariant();
    }

    public DynamicEntityGroupBuilder<TEntity> WithOperation(DynamicApiOperation apiOperation)
    {
        _operations.Add(apiOperation);
        return this;
    }

    [Obsolete]
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
            var name = ExpressionUtils.GetPropertyName(prop);
            _includeProperties.Add(name);
        }
        return this;
    }

    public DynamicEntityGroupBuilder<TEntity> ExcludeProperties(params Expression<Func<TEntity, object>>[] properties)
    {
        foreach (var prop in properties)
        {
            var name = ExpressionUtils.GetPropertyName(prop);
            _excludeProperties.Add(name);
        }
        return this;
    }
}
