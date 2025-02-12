global using CFW.EntityApi.Attributes;

using CFW.EntityApi;
using CFW.EntityApi.Models;
using CFW.EntityApi.Queries;
using CFW.EntityApi.TestApi;
using CFW.EntityApi.TestApi.Infrastructures.DbContexts;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.Options;
using Microsoft.OData.ModelBuilder;
using Scalar.AspNetCore;


var builder = WebApplication.CreateBuilder(args);
builder.Services.AddOpenApi(o =>
{
    o.AddOperationTransformer<OpenApiQueryOperationTransformer>();
});

builder.Services.AddOptions<DbContextSetting>()
    .Bind(builder.Configuration.GetSection(nameof(DbContextSetting)));

builder.Services.AddDbContext<AppDbContext>(
               (s, options) =>
               {
                   options.EnableSensitiveDataLogging()
                   .ReplaceService<IModelCustomizer, AutoScanModelCustomizer<AppDbContext>>();

                   var dbContextSetting = s.GetRequiredService<IOptions<DbContextSetting>>().Value;
                   dbContextSetting.Configure(options);
               });

builder.Services
        .AddEntityApi(Constants.DefaultODataRoutePrefix).ConfigureODataModelBuilder(b => b.EnableLowerCamelCase())
        .UseDbContext<AppDbContext>();

var app = builder.Build();

app.UseEntityApi();

app.MapOpenApi();

app.MigrateDatabase();

//https://github.com/dotnet/aspnetcore/issues/57332#issuecomment-2480939916
app.MapScalarApiReference(_ => _.Servers = []);

app.Run();
