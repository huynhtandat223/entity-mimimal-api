using CFW.AppHost.Infrastructures.IXBrowserGateway;
using Refit;
using System.Text.Json;

namespace CFW.AppHost.Features.BrowserProfiles;

public class Class
{
    public void bk()
    {
        //Refit for IXBrowserGateway
        //var refitSettings = new RefitSettings
        //{
        //    ContentSerializer = new SystemTextJsonContentSerializer(new JsonSerializerOptions
        //    {
        //        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        //        PropertyNameCaseInsensitive = true
        //    })
        //};
        //builder.Services
        //    .AddRefitClient<IProfileGateway>(refitSettings)
        //    .ConfigureHttpClient(c => c.BaseAddress = new Uri("http://127.0.0.1:53200/api/v2"));

    }
}
