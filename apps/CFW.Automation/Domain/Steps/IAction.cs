using CFW.Core.Results;

namespace CFW.Automation.Domain.Steps;

public interface IAction
{
    public Task<Result> Execute(AutomationContext context);
}
