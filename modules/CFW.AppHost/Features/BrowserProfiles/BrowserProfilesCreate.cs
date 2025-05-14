using CFW.AppHost.Infrastructures.IXBrowserGateway;
using CFW.AppHost.Infrastructures.IXBrowserGateway.Models;
using CFW.DynamicApi;

namespace CFW.AppHost.Features.BrowserProfiles;

[ApiOperation("browser-profiles", RouteName = "create")]
public class BrowserProfilesCreate : IRequestHandler<CreateProfileRequest, CreateProfileResponse>
{
    public async Task<IResult<CreateProfileResponse>> Handle(RequestModel<CreateProfileRequest> request, CancellationToken cancellationToken)
    {
        var profileGateway = request.ServiceProvider.GetRequiredService<IProfileGateway>();

        var result = new CreateProfileResponse();
        result = await profileGateway.CreateProfileAsync(request.Model);

        if (result.Error?.Code != 0)
        {
            return result.Failed(result.Error!.ToJsonString());
        }
        return result.Success();
    }
}