using Hangfire.Server;
using Hangfire.States;

namespace CFW.HangfireExtentions.Models;

public class JobMetadata
{
    public string? JobName { set; get; }

    public string? Queue { get; set; }

    public IEnumerable<string> Tags { get; set; } = new List<string>();

    public TimeSpan ScheduleTimes { set; get; } = TimeSpan.FromSeconds(5);
}

public class JobMetadata<TNotification> : JobMetadata
{
    public TNotification? Notification { set; get; }
}

public class JobPerformInstance<TNotification>
{
    public string? JobId { set; get; }

    public JobContext<TNotification> JobContext { get; set; } = new JobContext<TNotification>();

    public PerformingContext PerformingContext { get; set; } = default!;

    public PerformedContext PerformedContext { get; set; } = default!;

    public FailedState FailedState { get; set; } = default!;

    public SucceededState SucceededState { get; set; } = default!;
}

public class JobContext<TNotification>
{
    public Guid CorrelationId { set; get; }

    public bool IsDevelopment { get; set; }

    public bool RequestByInternalServices { get; set; }

    public JobMetadata<TNotification>? JobMetadata { get; set; }

    public bool? IsSuccess { get; set; }

    public object? Result { get; set; }
}
