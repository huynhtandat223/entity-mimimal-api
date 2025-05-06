using System.Linq.Expressions;
using System.Reflection;

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

    /// <summary>
    /// Get property name of expression
    /// </summary>
    /// <typeparam name="TEntity"></typeparam>
    /// <param name="expression"></param>
    /// <returns></returns>
    /// <exception cref="InvalidOperationException"></exception>
    public static string GetPropertyName(LambdaExpression expression)
    {
        if (expression.Body is MemberExpression member)
            return member.Member.Name;
        if (expression.Body is UnaryExpression unary && unary.Operand is MemberExpression memberOperand)
            return memberOperand.Member.Name;
        throw new InvalidOperationException("Invalid expression");
    }

    public static Expression<Func<TEntity, TEntity>> BuildSelector<TEntity>(IEnumerable<string> propertyNames)
    {
        var parameter = Expression.Parameter(typeof(TEntity), "entity");

        var bindings = propertyNames
            .Select(name =>
            {
                var property = typeof(TEntity).GetProperty(name, BindingFlags.IgnoreCase | BindingFlags.Public | BindingFlags.Instance);
                if (property == null || !property.CanWrite)
                    throw new ArgumentException($"Property '{name}' not found or not writable on type '{typeof(TEntity)}'");

                var propertyAccess = Expression.Property(parameter, property);
                return Expression.Bind(property, propertyAccess);
            })
            .ToList();

        var body = Expression.MemberInit(Expression.New(typeof(TEntity)), bindings);

        return Expression.Lambda<Func<TEntity, TEntity>>(body, parameter);
    }

}
