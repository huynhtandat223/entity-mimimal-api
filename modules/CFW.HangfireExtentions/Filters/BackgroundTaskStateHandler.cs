using CFW.Core.Results;
using CFW.Core.Utils;
using CFW.HangfireExtentions.Models;
using Hangfire;
using Hangfire.States;

namespace CFW.HangfireExtentions.Filters;

public class BackgroundTaskStateHandler : IElectStateFilter
{
    public void OnStateElection(ElectStateContext context)
    {
        if (context.CandidateState is not SucceededState succeededState
            || succeededState.Result is null)
        {
            return;
        }

        if (succeededState.Result is not BackgroundJobResult
            && succeededState.Result is Result domainResult)
        {
            if (domainResult.IsNotSuccess())
            {
                context.CandidateState = new FailedState(new IgnoreRetryException(domainResult.ToJsonString()));
            }
        }

        if (succeededState.Result is not BackgroundJobResult backgroundJobResult)
        {
            return;
        }

        var isRetryable = backgroundJobResult.IsRetryable;
        if (isRetryable)
        {
            context.CandidateState = new FailedState(new Exception(backgroundJobResult.ToJsonString()));
            AutomaticRetry(context);
        }

        context.Connection.SetJobParameter(context.BackgroundJob.Id, "Result", backgroundJobResult.ToJsonString());
    }

    private void AutomaticRetry(ElectStateContext context)
    {
        new AutomaticRetryAttribute
        {
            Attempts = 5,
            DelayInSecondsByAttemptFunc = (attempt) => (int)Math.Pow(2, attempt) * 10
        }.OnStateElection(context);
    }
}
