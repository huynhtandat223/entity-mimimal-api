using CFW.HangfireExtentions.Models;

namespace CFW.HangfireExtentions;
public interface IBackgroundJobInterceptor
{
    Task OnPerforming(JobPerformInstance<object> jobPerformInstance, CancellationToken cancellationToken);

    Task OnPerformed(JobPerformInstance<object> jobPerformInstance, CancellationToken cancellationToken);

    Task OnFailed(JobPerformInstance<object> jobPerformInstance, CancellationToken cancellationToken);

    Task OnSuccess(JobPerformInstance<object> jobPerformInstance, CancellationToken cancellationToken);
}
