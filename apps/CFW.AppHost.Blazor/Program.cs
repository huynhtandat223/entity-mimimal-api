using Blazored.LocalStorage;
using CFW.AppHost.Blazor;
using CFW.AppHost.Blazor.ApiClient;
using CFW.AppHost.Blazor.Authentications;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Radzen;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

// Add Radzen services
builder.Services.AddRadzenComponents();
builder.Services.AddScoped<DialogService>();
builder.Services.AddScoped<NotificationService>();
builder.Services.AddScoped<TooltipService>();
builder.Services.AddScoped<ContextMenuService>();


// Add authentication services
builder.Services.AddAuthorizationCore();
builder.Services.AddCascadingAuthenticationState();

builder.Services.AddScoped<AuthenticationStateProvider, CustomAuthStateProvider>();
builder.Services.AddScoped<CustomAuthStateProvider>();

// Local storage
builder.Services.AddBlazoredLocalStorage();

//auth
builder.Services.AddScoped<AuthHeaderHandler>();

builder.Services.AddHttpClient<IApiClient, ApiClient>(http =>
{
    http.BaseAddress = new Uri("https://localhost:5001");
}).AddHttpMessageHandler<AuthHeaderHandler>();

await builder.Build().RunAsync();
