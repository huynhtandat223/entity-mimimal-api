global using CFW.Core.Results;
global using CFW.Core.Utils;
using CFW.AppHost.Features.Identity.Services.Extensions;
using CFW.AppHost.Features.Shared;
using CFW.AppHost.Infrastructures.IXBrowserGateway;
using CFW.AppHost.Infrastructures.RunTimeDevelopments;
using CFW.Core.Dependencies;
using CFW.DynamicApi.Entensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Refit;
using Serilog;
using System.Text.Json;

var builder = WebApplication.CreateBuilder(args);
var isTesting = builder.Environment.IsEnvironment("Testing");

//logging
Log.Logger = new LoggerConfiguration()
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .CreateLogger();
builder.Host.UseSerilog();

// Add Cors policy
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy
            .WithOrigins("http://localhost:3000") // 👈 your frontend URL
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials(); // if using cookies or auth headers
    });
});


// In case interation test: let test project setup services
if (!isTesting || true)
{
    builder.Services.AddDbContext<AppDbContext>(options =>
    options
    //.ReplaceService<IModelCacheKeyFactory, MyModelCacheKeyFactory>()
    .UseSqlite("Filename=database.db"));

    builder.Services.AddDbContext<RuntimeDbContext>(options =>
   options
   .ReplaceService<IModelCacheKeyFactory, MyModelCacheKeyFactory>()
   .UseSqlite("Filename=database.db"));

    builder.Services
        .AddDynamicApi("/api/v1/", container =>
        {
            container.DefaultPageSize = 50;
        });
}

//Authentication
//builder.Services.AddAuthorization();
//builder.Services.AddIdentityApiEndpoints<ApplicationUser>()
//    .AddRoles<ApplicationRole>()
//    .AddEntityFrameworkStores<AppDbContext>();


//Refit for IXBrowserGateway
var refitSettings = new RefitSettings
{
    ContentSerializer = new SystemTextJsonContentSerializer(new JsonSerializerOptions
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        PropertyNameCaseInsensitive = true
    })
};
builder.Services
    .AddRefitClient<IProfileGateway>(refitSettings)
    .ConfigureHttpClient(c => c.BaseAddress = new Uri("http://127.0.0.1:53200/api/v2"));

await builder.TryAddAllServicesAndInitModules();

var app = builder.Build();

//Use CORS policy
app.UseCors("AllowFrontend");

//app.UseAuthorization();
//app.MapIdentityApi<ApplicationUser>();

await app.UseDynamicApi();


using var scope = app.Services.CreateScope();
var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
await db.SeedSuperAdminAsync();

await app.RunModules();

app.Run();


internal sealed class MyModelCacheKeyFactory : IModelCacheKeyFactory
{
    public object Create(DbContext context, bool designTime)
    {
        return Guid.NewGuid();
    }
}