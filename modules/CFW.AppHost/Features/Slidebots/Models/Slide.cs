using CFW.Core.Entities;

namespace CFW.AppHost.Features.Slidebots.Models;

public class Slide : IEntity<Guid>
{
    public Guid Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }

    public string Steps { get; set; } = string.Empty;

    public bool IsActive { get; set; } = true;

    // Automation context
    public SlideStatus Status { get; set; } = SlideStatus.Pending;

    public DateTime? LastRunAt { get; set; }

    public TimeSpan? LastDuration { get; set; }

    public string? BrowserProfileId { get; set; }

    // Output
    public string? OutputPath { get; set; }

    public string? Logs { get; set; }
}


public enum SlideStatus
{
    Pending,
    Running,
    Succeeded,
    Failed,
    Cancelled
}

