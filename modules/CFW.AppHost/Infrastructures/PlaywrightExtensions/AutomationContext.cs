using Microsoft.Playwright;

namespace CFW.AppHost.Infrastructures.PlaywrightExtensions;

public class AutomationContext : IDisposable
{
    public IPage Page { set; get; }

    private AutomationContext(IPage page)
    {
        Page = page;
    }

    public void Dispose()
    {
        Page?.CloseAsync();
    }

    public static async Task<AutomationContext> CreateAsync(string wsEndpoint)
    {
        var playwright = await Playwright.CreateAsync();
        var browser = await playwright.Chromium.ConnectOverCDPAsync(wsEndpoint);
        var context = await browser.NewContextAsync();
        var page = await context.NewPageAsync();

        return new AutomationContext(page);
    }

    public async Task Execute(IStep[] steps)
    {
        foreach (var step in steps)
        {
            await step.Execute(this);
        }
    }
}
