using CFW.Core.Dependencies;
using CFW.DynamicApi.Entensions;
using Microsoft.EntityFrameworkCore;

namespace CFW.AppHost.Features.Core.Configurations;

public class DynamicApiModuleInitializer : IModuleInitializer
{
    public async Task InitModule(IHostApplicationBuilder builder)
    {
        var isTesting = builder.Environment.IsEnvironment("Testing");
        if (!isTesting)
        {
            builder.Services.AddDbContext<AppDbContext>(options => options.UseSqlite("Filename=database.db"));
            builder.Services.AddDynamicApi("/api/v1/");
        }

        await Task.CompletedTask;
    }

    public async Task RunModule(WebApplication app)
    {
        await app.UseDynamicApi();
    }
}

