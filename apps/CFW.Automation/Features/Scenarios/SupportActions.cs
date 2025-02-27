using CFW.Automation.Domain.Steps;
using CFW.Automation.Models;
using CFW.Core.Results;
using CFW.EntityApi.Attributes;
using CFW.EntityApi.Intefaces;

namespace CFW.Automation.Features.Scenarios;

[EntityAction("scenarios/supportActions")]
public class SupportActions : IOperationHandler<SupportActions.Request, List<AutomationAction>>
{
    public record Request
    {
    }

    public async Task<Result<List<AutomationAction>>> Handle(Request request, CancellationToken cancellationToken)
    {
        var actionTypes = AppDomain.CurrentDomain.GetAssemblies()
            .SelectMany(s => s.GetTypes())
            .Where(p => typeof(IAction).IsAssignableFrom(p) && !p.IsInterface && !p.IsAbstract);

        var actions = actionTypes.Select(x => new AutomationAction
        {
            Type = x.FullName!,
            Properties = "{}"
        }).ToList();
        return actions.Success();
    }
}
