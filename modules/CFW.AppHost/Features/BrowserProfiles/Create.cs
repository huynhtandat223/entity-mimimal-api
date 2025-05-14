using CFW.AppHost.Infrastructures.IXBrowserGateway;
using CFW.AppHost.Infrastructures.IXBrowserGateway.Models;
using CFW.DynamicApi;
using Refit;

namespace CFW.AppHost.Features.BrowserProfiles;

[ApiOperation("browser-profiles", RouteName = "create")]
public class BrowserProfilesCreate : IRequestHandler<CreateProfileRequest, CreateProfileResponse>
{
    public async Task<IResult<CreateProfileResponse>> Handle(RequestModel<CreateProfileRequest> request, CancellationToken cancellationToken)
    {
        var result = new CreateProfileResponse();
        var gitHubApi = RestService.For<IProfileGateway>("http://127.0.0.1:53200/api/v2");
        result = await gitHubApi.CreateProfileAsync(request.Model);

        if (result.Error?.Code != 0)
        {
            return result.Failed(result.Error!.ToJsonString());
        }
        return result.Success();
    }
}