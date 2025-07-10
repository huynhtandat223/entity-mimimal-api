using CFW.HangfireExtentions.Models;
using Hangfire;
using Hangfire.Server;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace CFW.HangfireExtentions.Filters;
public class BackgroundJobActivator : JobActivator
{
    private readonly IServiceProvider _serviceProvider;

    public BackgroundJobActivator(IHost host)
    {
        _serviceProvider = host.Services;
    }

    public override JobActivatorScope BeginScope(PerformContext context)
    {
        return new CustomJobActivatorScope(_serviceProvider, context);
    }
}

internal class CustomJobActivatorScope : JobActivatorScope
{
    private readonly IServiceScope _serviceScope;
    private readonly PerformContext _context;

    public CustomJobActivatorScope(IServiceProvider serviceProvider, PerformContext context)
    {
        _serviceScope = serviceProvider.CreateScope();
        _context = context;
    }

    public override object Resolve(Type type)
    {
        var jobContext = _context.GetJobParameter<JobContext<object>>(BackgroundTask.ContextKey);
        var context = _serviceScope.ServiceProvider.GetRequiredService<IRequestContext>();
        //context.SetUserProfile(jobContext?.UserProfile);
        var service = _serviceScope.ServiceProvider.GetRequiredService(type);
        return service;
    }

    public override void DisposeScope()
    {
        _serviceScope.Dispose();
    }
}
