using CFW.AppHost.Features.Identity.Models;
using CFW.AppHost.Features.Identity.Services.Extensions;
using CFW.AppHost.Features.Shared;
using CFW.DynamicApi;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services
    .AddDynamicApi("/api/v1/", container =>
    {
        container.DefaultPageSize = 50;
    });

//Authentication
builder.Services.AddAuthorization();
builder.Services.AddIdentityApiEndpoints<ApplicationUser>()
    .AddRoles<ApplicationRole>()
    .AddEntityFrameworkStores<AppDbContext>();

var app = builder.Build();

app.UseAuthorization();
app.MapIdentityApi<ApplicationUser>();

await app.UseDynamicApi();

using var scope = app.Services.CreateScope();
var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
await db.SeedSuperAdminAsync();

app.Run();


