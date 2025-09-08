//using CFW.AppHost.Infrastructures.IXBrowserGateway;
//using CFW.AppHost.Infrastructures.IXBrowserGateway.Models;
//using CFW.DynamicApi;

//namespace CFW.AppHost.Features.BrowserProfiles;

//[ApiOperation("browser-profiles", RouteName = "list")]
//public class BrowserProfilesList : IRequestHandler<ProfileListRequest, ProfileListData>
//{
//    private readonly IProfileGateway _profileGateway;

//    public BrowserProfilesList(IProfileGateway profileGateway)
//    {
//        _profileGateway = profileGateway;
//    }

//    public async Task<IResult<ProfileListData>> Handle(RequestModel<ProfileListRequest> request, CancellationToken cancellationToken)
//    {
//        var result = new ProfileListData();
//        var profiles = await _profileGateway.GetProfileListAsync(request.Model);

//        if (profiles.Error?.Code != 0)
//        {
//            return result.Failed(profiles.Error!.ToJsonString());
//        }

//        result = profiles.Data;
//        return result.Success();
//    }
//}
