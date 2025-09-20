using CFW.Core.Dependencies;
using Serilog;

namespace CFW.AppHost.Features.Core.Configurations;

public class BasicModuleInitializer : IModuleInitializer
{
    public async Task InitModule(IHostApplicationBuilder builder)
    {
        //logging
        Log.Logger = new LoggerConfiguration()
            .Enrich.FromLogContext()
            .WriteTo.Console()
            .CreateLogger();
        ((WebApplicationBuilder)builder).Host.UseSerilog();

        // Add Cors policy
        builder.Services.AddCors(options =>
        {
            options.AddPolicy("AllowFrontend", policy =>
            {
                policy
                    .WithOrigins("http://localhost:3000", "http://localhost:3001", "https://localhost:3002") // 👈 your frontend URL
                    .AllowAnyHeader()
                    .AllowAnyMethod()
                    .AllowCredentials(); // if using cookies or auth headers
            });
        });

        await Task.CompletedTask;
    }

    public async Task RunModule(WebApplication app)
    {
        app.UseCors("AllowFrontend");

        await Task.CompletedTask;
    }
}

