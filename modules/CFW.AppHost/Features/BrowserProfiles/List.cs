using CFW.AppHost.Infrastructures.IXBrowserGateway;
using CFW.AppHost.Infrastructures.IXBrowserGateway.Models;
using CFW.Core.Utils;
using CFW.DynamicApi;

namespace CFW.AppHost.Features.BrowserProfiles;

[ApiOperation("browser-profiles", RouteName = "list")]
public class BrowserProfilesList : IApiOperationHandler<ProfileListRequest, ProfileListData>
{
    private readonly IProfileGateway _profileGateway;

    public BrowserProfilesList(IProfileGateway profileGateway)
    {
        _profileGateway = profileGateway;
    }

    public async Task<IResult<ProfileListData>> Handle(ProfileListRequest request, CancellationToken cancellationToken)
    {
        var result = new ProfileListData();
        var profiles = await _profileGateway.GetProfileListAsync(request);

        if (profiles.Error?.Code != 0)
        {
            return result.Failed(profiles.Error!.ToJsonString());
        }

        result = profiles.Data;
        return result.Success();
    }
}
