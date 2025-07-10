using CFW.Core.Results;
using CFW.Core.Utils;
using CFW.HangfireExtentions.Models;
using Hangfire;
using Hangfire.Common;
using Hangfire.States;
using Hangfire.Tags;
using System.Linq.Expressions;

namespace CFW.HangfireExtentions;

public class BackgroundTask
{
    private readonly IRequestContext _requestContext;

    public const string ContextKey = "context";
    public const string ContinuationsKey = "Continuations";

    public BackgroundTask(IRequestContext requestContext)
    {
        _requestContext = requestContext;
    }

    internal BackgroundTask<TDependency, TValue> Create<TDependency, TValue>(Expression<Func<TDependency
        , Task<TValue>>> methodCall)
    {
        return new BackgroundTask<TDependency, TValue>(_requestContext, methodCall);
    }

    public IResult Requeue(string jobId)
    {
        var _backgroundJobClient = new BackgroundJobClient();
        var connection = _backgroundJobClient.Storage.GetConnection();
        var jobData = connection.GetJobData(jobId);

        if (jobData == null)
        {
            return this.Notfound();
        }

        _backgroundJobClient.Requeue(jobId);
        return this.Success();
    }
}

internal class BackgroundTask<TDependency, TValue>
{
    private readonly IRequestContext _requestContext;

    private readonly Expression<Func<TDependency, Task<TValue>>> _methodCall;
    private readonly List<LambdaExpression> _continueJobs;

    public BackgroundTask(IRequestContext _requestContext, Expression<Func<TDependency, Task<TValue>>> methodCall)
    {
        this._requestContext = _requestContext;
        _methodCall = methodCall;

        _continueJobs = new List<LambdaExpression>();
    }

    public BackgroundTask<TDependency, TValue> ContinueWith<TDependency2>(Expression<Func<TDependency2, Task>> methodCall)
    {
        _continueJobs.Add(methodCall);
        return this;
    }

    public void Enqueue(JobMetadata jobMetadata)
    {
        var _backgroundJobClient = new BackgroundJobClient();

        //var userProfile = _requestContext.GetUserProfile();
        var context = new JobContext<object>
        {
            CorrelationId = Guid.NewGuid(),
            //JobMetadata = jobMetadata,
            IsDevelopment = _requestContext.IsDevelopment(),
            //RequestByInternalServices = _requestContext.RequestByInternalServices,
            //UserProfile = userProfile
        };

        var parameters = new Dictionary<string, object>
            {
                { BackgroundTask.ContextKey, context }
            };

        var queue = jobMetadata.Queue ?? "default";
        var job = _methodCall.CreateJobFromExpression(queue ?? string.Empty);
        var jobId = _backgroundJobClient.Create(job, new ScheduledState(jobMetadata.ScheduleTimes), parameters);

        jobId.AddTags(jobMetadata.Tags);
        jobId.AddTags(jobMetadata.JobName);

        var methodName = job.Method?.Name;
        var target = job.Method?.DeclaringType?.Name;
        if (methodName.IsNotNullOrEmpty() && target.IsNotNullOrEmpty())
        {
            jobId.AddTags($"{target}.{methodName}");
        }

        foreach (var continueJobExpr in _continueJobs)
        {
            var method = continueJobExpr.Body as MethodCallExpression;
            var methodInfo = method!.Method;
            var type = methodInfo.DeclaringType;

            var args = method.Arguments.Select(x => (object?)null).ToArray();

            for (int i = 0; i < method.Arguments.Count; i++)
            {
                var arg = method.Arguments[i];
                if (arg is MemberExpression memberExpression && memberExpression.Member.DeclaringType == typeof(TValue))
                {
                    args[i] = default;
                }
                else
                {
                    args[i] = job.GetExpressionValue(arg);
                }
            }

            var nextState = new EnqueuedState { Queue = queue };
            var state = new AwaitingState(jobId, nextState, JobContinuationOptions.OnlyOnSucceededState);

            var continueJob = new Job(type, methodInfo, args, queue);

            var continueJobId = _backgroundJobClient.Create(continueJob, state, parameters);

            continueJobId.AddTags(jobMetadata.Tags);
            continueJobId.AddTags(jobMetadata.JobName);

            var continueMethodName = job.Method?.Name;
            var continueTarget = job.Method?.DeclaringType?.Name;
            if (continueMethodName.IsNotNullOrEmpty() && continueTarget.IsNotNullOrEmpty())
            {
                continueJobId.AddTags($"{continueTarget}.{continueMethodName}");
            }

            jobId = continueJobId;
        }
    }
}