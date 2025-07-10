using CFW.HangfireExtentions.Models;
using Hangfire.Server;
using Hangfire.States;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace CFW.HangfireExtentions.Filters;
public class InterceptorFilter : IServerFilter, IElectStateFilter
{
    private readonly IServiceProvider _serviceProvider;

    public InterceptorFilter(IHost host)
    {
        _serviceProvider = host.Services;
    }

    public void OnPerformed(PerformedContext context)
    {
        using var scope = _serviceProvider.CreateScope();
        var interceptor = scope.ServiceProvider.GetService<IBackgroundJobInterceptor>();
        if (interceptor == null) return;

        var jobContext = context.GetJobParameter<JobContext<object>>(BackgroundTask.ContextKey);
        var jobPerformInstance = new JobPerformInstance<object>
        {
            JobContext = jobContext,
            PerformedContext = context
        };

        interceptor.OnPerformed(jobPerformInstance, context.CancellationToken.ShutdownToken).Wait();
    }

    public void OnPerforming(PerformingContext context)
    {
        using var scope = _serviceProvider.CreateScope();
        var interceptor = scope.ServiceProvider.GetService<IBackgroundJobInterceptor>();
        if (interceptor == null) return;

        var jobId = context.BackgroundJob.Id;
        var jobContext = context.GetJobParameter<JobContext<object>>(BackgroundTask.ContextKey);
        var jobPerformInstance = new JobPerformInstance<object>
        {
            JobId = jobId,
            JobContext = jobContext,
            PerformingContext = context
        };

        interceptor.OnPerforming(jobPerformInstance, context.CancellationToken.ShutdownToken).Wait();
    }

    public void OnStateElection(ElectStateContext context)
    {
        if (context.CandidateState is not FailedState
            && context.CandidateState is not SucceededState)
        {
            return;
        }

        using var scope = _serviceProvider.CreateScope();
        var interceptor = scope.ServiceProvider.GetService<IBackgroundJobInterceptor>();
        if (interceptor == null) return;

        var jobContext = context.GetJobParameter<JobContext<object>>(BackgroundTask.ContextKey);
        var jobPerformInstance = new JobPerformInstance<object>
        {
            JobContext = jobContext,
        };

        if (context.CandidateState is FailedState failedState)
        {
            interceptor.OnFailed(jobPerformInstance, default).Wait();
        }

        if (context.CandidateState is SucceededState succeededState)
        {
            jobPerformInstance.SucceededState = succeededState;
            var hasContinuations = context.BackgroundJob.ParametersSnapshot.ContainsKey(BackgroundTask.ContinuationsKey);
            if (hasContinuations)
            {
                return;
            }
            interceptor.OnSuccess(jobPerformInstance, default).Wait();
        }

    }
}
