using CFW.EntityApi;
using CFW.ServiceDefaults;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();


builder.Services
    .AddEntityMinimalApi("api")
    .PopuplateEntityFrameworkEntities<DefaultDbContext>(o => o.UseSqlite("Data Source=auto-api.db"));

var app = builder.Build();

app.MapDefaultEndpoints();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.UseEntityMinimalApi();

using var scope = app.Services.CreateScope();
var db = scope.ServiceProvider.GetService<DefaultDbContext>();
if (db is not null && !db.Database.CanConnect())
{
    db.Database.EnsureCreated();
}

app.Run();