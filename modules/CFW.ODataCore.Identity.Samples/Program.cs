using CFW.ODataCore;
using CFW.ODataCore.Identity;
using CFW.ODataCore.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.OData.ModelBuilder;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<CoreIdentityDbContext>(
               options => options
               .EnableSensitiveDataLogging()
                .ReplaceService<IModelCustomizer, AutoScanModelCustomizer<CoreIdentityDbContext>>()
                .UseSqlite($@"Data Source=appdbcontext.db"));

//Add entity minimal api
builder.Services.AddEntityMinimalApi(o => o
    .ConfigureODataModelBuilder(b => b.EnableLowerCamelCase())
    .UseDefaultDbContext<CoreIdentityDbContext>(o =>
    {
        o.AutoGenerateEndpoints = new AutoGenerationEndpointsOptions();
    }));

//Add swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(o =>
{

    //<see cref="https://stackoverflow.com/questions/61881770/invalidoperationexception-cant-use-schemaid-the-same-schemaid-is-already-us" >
    o.CustomSchemaIds(type => type.FullName);
});


var app = builder.Build();

app.UseHttpsRedirection();

//use swagger
app.UseSwagger();
app.UseSwaggerUI();

//Use entity minimal api
app.UseEntityMinimalApi();

app.Run();
