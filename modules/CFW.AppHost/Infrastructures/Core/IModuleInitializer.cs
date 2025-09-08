using Microsoft.Extensions.Hosting;

namespace CFW.Core.Dependencies;
public interface IModuleInitializer
{
    public Task InitModule(IHostApplicationBuilder builder) => Task.CompletedTask;

    public Task RunModule(WebApplication app) => Task.CompletedTask;
}
