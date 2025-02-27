using CFW.Core.Results;

namespace CFW.Automation.Domain.Steps;

public class GoTo : IAction
{
    public string Url { set; get; } = string.Empty;

    public async Task<Result> Execute(AutomationContext context)
    {
        var page = context.Page;
        var result = await page.GotoAsync(Url);
        return result.Success();
    }
}

public class ClickStep : IAction
{
    public string Selector { set; get; } = string.Empty;

    public async Task<Result> Execute(AutomationContext context)
    {
        var page = context.Page;
        await page.ClickAsync(Selector);

        return this.Success();
    }
}

public class PressStep : IAction
{
    public string Key { set; get; } = string.Empty;
    public async Task<Result> Execute(AutomationContext context)
    {
        var page = context.Page;
        await page.Keyboard.PressAsync(Key);
        return this.Success();
    }
}

public class FillStep : IAction
{
    public string Selector { set; get; } = string.Empty;

    public string Value { set; get; } = string.Empty;

    public async Task<Result> Execute(AutomationContext context)
    {
        try
        {
            var page = context.Page;
            var locator = page.Locator(Selector);

            await locator.FillAsync(Value);
            return this.Success();
        }
        catch (Exception ex)
        {

            throw;
        }

    }
}