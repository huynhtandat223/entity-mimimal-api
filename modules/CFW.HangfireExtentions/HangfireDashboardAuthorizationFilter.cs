using Hangfire.Dashboard;
using System.Diagnostics.CodeAnalysis;

namespace CFW.HangfireExtentions;

/// <summary>
/// In dev or debug mode, dashboard authorization filter that allows all users to access the Hangfire dashboard. But in production, you should implement your own authorization logic.
/// </summary>
public class HangfireDashboardAuthorizationFilter : IDashboardAuthorizationFilter
{
    public bool Authorize([NotNull] DashboardContext context)
    {
        return true;
    }
}