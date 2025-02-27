using Microsoft.Playwright;

namespace CFW.Automation.Domain;

public class AutomationContext
{
    public IPage Page { get; set; } = null!;
}
