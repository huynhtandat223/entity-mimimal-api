global using CFW.Core.Results;
global using CFW.Core.Utils;
using CFW.AppHost.Features.Identity.Services.Extensions;
using CFW.AppHost.Features.Shared;
using CFW.AppHost.Infrastructures.IXBrowserGateway;
using CFW.DynamicApi;
using Microsoft.EntityFrameworkCore;
using Refit;
using System.Text.Json;

var builder = WebApplication.CreateBuilder(args);

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

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite("Filename=database.db"));

builder.Services
    .AddDynamicApi("/api/v1/", container =>
    {
        container.DefaultPageSize = 50;
    });

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

var app = builder.Build();

//Use CORS policy
app.UseCors("AllowFrontend"); // 👈 apply named policy globally

//app.UseAuthorization();
//app.MapIdentityApi<ApplicationUser>();

await app.UseDynamicApi();


using var scope = app.Services.CreateScope();
var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
await db.SeedSuperAdminAsync();

app.Run();


