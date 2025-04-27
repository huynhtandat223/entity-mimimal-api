using CFW.AppHost.Features.Shared;
using CFW.DynamicApi;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services
    .AddDynamicApi("api/v1", container =>
    {
        container.DefaultPageSize = 50;
    });

var app = builder.Build();

app.UseDynamicApi();

using var scope = app.Services.CreateScope();
var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
await db.Database.EnsureCreatedAsync();

app.Run();


