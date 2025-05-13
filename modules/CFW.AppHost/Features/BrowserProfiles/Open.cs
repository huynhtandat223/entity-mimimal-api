using CFW.AppHost.Infrastructures.IXBrowserGateway;
using CFW.AppHost.Infrastructures.IXBrowserGateway.Models;
using CFW.DynamicApi;
using Microsoft.Playwright;

namespace CFW.AppHost.Features.BrowserProfiles;

[ApiOperation("browser-profiles", RouteName = "open")]
public class BrowserProfilesOpen : IApiOperationHandler<OpenProfileRequest, OpenProfileResponse>
{
    private readonly IProfileGateway _profileGateway;

    public BrowserProfilesOpen(IProfileGateway profileGateway)
    {
        _profileGateway = profileGateway;
    }

    public async Task<IResult<OpenProfileResponse>> Handle(OpenProfileRequest request, CancellationToken cancellationToken)
    {
        var result = await _profileGateway.OpenProfileAsync(request);

        if (result.Error?.Code != 0)
        {
            return result.Failed(result.Error!.ToJsonString());
        }

        var playwright = await Playwright.CreateAsync();
        var browser = await playwright.Chromium.ConnectOverCDPAsync(result.Data.Ws);
        var context = await browser.NewContextAsync();
        var page = await context.NewPageAsync();
        await page.GotoAsync("https://example.com");

        Console.WriteLine(await page.TitleAsync());

        return result.Success();
    }
}
