namespace CFW.AppHost.Infrastructures.PlaywrightExtensions;

public interface IStep
{
    Task Execute(AutomationContext context);
}

public class GotoStep : IStep
{
    public string Url { set; get; } = string.Empty;

    public async Task Execute(AutomationContext context)
    {
        var page = context.Page;
        await page.GotoAsync(Url);
    }
}

public class ClickStep : IStep
{
    public string Selector { set; get; } = string.Empty;
    public async Task Execute(AutomationContext context)
    {
        var page = context.Page;
        await page.Locator(Selector).ClickAsync();
    }
}

public class TypeStep : IStep
{
    public string Selector { set; get; } = string.Empty;

    public string Text { set; get; } = string.Empty;

    public async Task Execute(AutomationContext context)
    {
        var page = context.Page;
        await page.Locator(Selector).FillAsync(Text);
    }
}
