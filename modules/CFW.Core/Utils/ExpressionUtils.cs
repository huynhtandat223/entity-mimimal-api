using System.Linq.Expressions;

namespace CFW.Core.Utils;
public static class ExpressionUtils
{
    /// <summary>
    /// Build equal expression : x => x.keyPropertyName == key
    /// </summary>
    /// <typeparam name="TSource"></typeparam>
    /// <param name="key"></param>
    /// <param name="keyPropertyName"></param>
    /// <returns></returns>
    public static Expression<Func<TSource, bool>> BuilderEqualExpression<TSource>(object key, string keyPropertyName)
    {
        //build equal expression
        var parameter = Expression.Parameter(typeof(TSource), "x");
        var propertyExpr = Expression.Property(parameter, keyPropertyName);

        var valueExpr = Expression.Constant(key);
        var equal = Expression.Equal(propertyExpr, valueExpr);
        var predicate = Expression.Lambda<Func<TSource, bool>>(equal, parameter);

        return predicate;
    }
}
