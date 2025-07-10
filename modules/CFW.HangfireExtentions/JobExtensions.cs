using Hangfire.Common;
using System.Linq.Expressions;
using System.Reflection;

namespace CFW.HangfireExtentions;
public static class JobExtensions
{
    public static object? GetExpressionValue(this Job job, Expression value)
    {
        if (_getParameterFunc == null)
        {
            var methodInfo = job.GetType().GetMethod("GetExpressionValue", BindingFlags.NonPublic | BindingFlags.Static);
            if (methodInfo == null)
            {
                throw new InvalidOperationException("Cannot find method GetExpressionValue");
            }

            var pValue = Expression.Parameter(typeof(Expression), "value");
            var methodCallExpression = Expression.Call(methodInfo, pValue);
            var lambda = Expression.Lambda<Func<Expression, object?>>(methodCallExpression, pValue);

            _getParameterFunc = lambda.Compile();
        }

        try
        {
            return _getParameterFunc(value);
        }
        catch
        {
            return null;
        }
    }
    private static Func<Expression, object?>? _getParameterFunc;


    public static Job CreateJobFromExpression<TDependency, TValue>(this Expression<Func<TDependency, Task<TValue>>> methodCall, string queue)
    {
        if (_jobFactory == null)
        {
            var methodInfo = typeof(Job).GetMethod("FromExpression", BindingFlags.NonPublic | BindingFlags.Static);
            if (methodInfo == null)
            {
                throw new InvalidOperationException("Cannot find method FromExpression");
            }

            var pMethodCall = Expression.Parameter(typeof(LambdaExpression), "methodCall");
            var pType = Expression.Parameter(typeof(Type), "type");
            var pQueue = Expression.Parameter(typeof(string), "queue");

            var methodCallExpression = Expression.Call(methodInfo, pMethodCall, pType, pQueue);
            var lambda = Expression.Lambda<Func<LambdaExpression, Type, string, Job>>(methodCallExpression, pMethodCall, pType, pQueue);
            _jobFactory = lambda.Compile();
        }

        return _jobFactory(methodCall, typeof(TDependency), queue);
    }

    private static Func<LambdaExpression, Type, string, Job>? _jobFactory;

}
