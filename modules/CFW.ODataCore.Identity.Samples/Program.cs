using CFW.ODataCore;
using CFW.ODataCore.Identity;
using CFW.ODataCore.Identity.Tenants.Models;
using CFW.ODataCore.Identity.Users.Models;
using CFW.ODataCore.Models;
using Microsoft.AspNetCore.Identity;
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
    .ConfigureMinimalApiContainerRouteGroup(containerGroupBuilder =>
    {
        containerGroupBuilder.RequireAuthorization();
    })
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

    o.MapType<Tenant>(() =>
    {
        return new Microsoft.OpenApi.Models.OpenApiSchema
        {
            Type = "object",
            Properties = new Dictionary<string, Microsoft.OpenApi.Models.OpenApiSchema>
            {
                ["id"] = new Microsoft.OpenApi.Models.OpenApiSchema { Type = "string" },
                ["name"] = new Microsoft.OpenApi.Models.OpenApiSchema { Type = "string" },
                ["displayName"] = new Microsoft.OpenApi.Models.OpenApiSchema { Type = "string" }
            }
        };

    });
});

// Add CORS services
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowSpecificOrigins", policy =>
    {
        policy.WithOrigins("http://localhost:3000")
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});


//Authentication
builder.Services.AddAuthorization();
builder.Services.AddIdentityApiEndpoints<User>()
    .AddRoles<IdentityRole<Guid>>()
    .AddEntityFrameworkStores<CoreIdentityDbContext>();

var app = builder.Build();

app.UseCors("AllowSpecificOrigins");

app.UseAuthorization();
app.MapIdentityApi<User>();

//use swagger
app.UseSwagger();
app.UseSwaggerUI();

//Use entity minimal api
app.UseEntityMinimalApi();


using var scope = app.Services.CreateScope();
var db = scope.ServiceProvider.GetService<CoreIdentityDbContext>();
if (db is not null && !db.Database.CanConnect())
{
    db.Database.EnsureCreated();
    var supperAdminName = "admin@gmail.com";
    var supperAdminRole = "SuperAdmin";
    var supperAdminPassword = "123!@#abcABC";

    using var userManager = scope.ServiceProvider.GetRequiredService<UserManager<User>>();
    using var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole<Guid>>>();

    var supperAdminUser = new User { UserName = supperAdminName, Email = supperAdminName };
    var result = await userManager.CreateAsync(supperAdminUser, supperAdminPassword);
    if (!result.Succeeded)
    {
        throw new InvalidOperationException("Test data invalid. User creation failed.");
    }

    var role = await roleManager.CreateAsync(new IdentityRole<Guid>(supperAdminRole));
    if (!role.Succeeded)
    {
        throw new InvalidOperationException("Test data invalid. Role creation failed.");
    }

    var addRoleResult = await userManager.AddToRoleAsync(supperAdminUser, supperAdminRole);
    if (!addRoleResult.Succeeded)
    {
        throw new InvalidOperationException("Test data invalid. Add role to user failed.");
    }
}


app.Run();
