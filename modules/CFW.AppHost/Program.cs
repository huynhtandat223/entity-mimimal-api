global using CFW.Core.Results;
global using CFW.Core.Utils;
using CFW.Core.Dependencies;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddHttpContextAccessor();

await builder.TryAddAllServicesAndInitModules();

var app = builder.Build();

await app.RunModules();


app.Run();