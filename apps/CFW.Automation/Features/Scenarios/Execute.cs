using CFW.Core.Results;
using CFW.EntityApi.Attributes;
using CFW.EntityApi.Intefaces;
using Microsoft.Playwright;
using static CFW.Automation.Features.Scenarios.Execute;

namespace CFW.Automation.Features.Scenarios;

[EntityAction("scenarios/{id}/execute")]
public class Execute : IOperationHandler<Request, Response>
{
    public record Request
    {
        public Guid Id { get; set; }
    }

    public record Response
    {
    }

    public async Task<Result<Response>> Handle(Request request, CancellationToken cancellationToken)
    {
        var result = new Response();
        using var playwright = await Playwright.CreateAsync();
        await using var browser = await playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions { Headless = false });
        var page = await browser.NewPageAsync();

        await page.GotoAsync("https://www.google.com");

        return result.Success();
    }
}
