using CFW.ODataCore.Intefaces;

namespace CFW.ODataCore.RouteMappers;

public static class RouteMapperExtensions
{
    /// <summary>
    /// https://learn.microsoft.com/en-us/aspnet/core/fundamentals/routing?view=aspnetcore-9.0
    /// </summary>
    private static readonly Dictionary<Type, string> _typeToConstraintMap = new()
    {
        { typeof(int), "int" },
        { typeof(bool), "bool" },
        { typeof(DateTime), "datetime" },
        { typeof(decimal), "decimal" },
        { typeof(double), "double" },
        { typeof(float), "float" },
        { typeof(Guid), "guid" },
        { typeof(long), "long" },
        { typeof(string), "alpha" } // Example: alpha for alphabetic strings
    };

    public static string GetKeyPattern<TKey>(this IRouteMapper routeMapper)
    {
        return _typeToConstraintMap.TryGetValue(typeof(TKey), out var constraint)
            ? $"{{key:{constraint}}}"
            : "{key}";
    }
}
