using CFW.Core.Entities;
using CFW.EntityApi.Attributes;

namespace CFW.Automation.Models;

[Entity("scenarios")]
public class Scenario : IEntity<Guid>
{
    public Guid Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; } = string.Empty;

    public bool IsEnabled { get; set; }

    public ICollection<AutomationAction> Actions { get; set; } = new List<AutomationAction>();
}

public class AutomationAction : IEntity<Guid>
{
    public Guid Id { get; set; }

    public string Type { get; set; } = string.Empty;

    public string Properties { get; set; } = string.Empty;
}