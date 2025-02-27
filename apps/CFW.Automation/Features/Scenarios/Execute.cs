using CFW.Automation.Domain;
using CFW.Automation.Domain.Steps;
using CFW.Automation.Models;
using CFW.Core.Results;
using CFW.Core.Utils;
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
        var gotoStep = new GoTo { Url = "https://www.google.com" };

        var scenario = new Scenario
        {
            Actions = new List<AutomationAction>
            {
                new AutomationAction{ Type = gotoStep.GetType().FullName!, Properties = gotoStep.ToJsonString() },
                new AutomationAction{ Type = typeof(FillStep).FullName!, Properties = new FillStep { Selector = "textarea[name='q']", Value = "Playwright" }.ToJsonString() },
                new AutomationAction{ Type = typeof(PressStep).FullName!, Properties = new PressStep { Key = "Enter" }.ToJsonString() }
            }
        };

        var result = new Response();
        using var playwright = await Playwright.CreateAsync();
        await using var browser = await playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions { Headless = false });
        var page = await browser.NewPageAsync();
        var context = new AutomationContext { Page = page };

        foreach (var action in scenario.Actions)
        {
            var type = Type.GetType(action.Type, true)!;

            var instance = action.Properties.JsonConvert(type) as IAction;

            await instance!.Execute(context);
        }

        return result.Success();
    }
}
