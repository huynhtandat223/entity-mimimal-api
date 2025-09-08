//using CFW.AppHost.Infrastructures.IXBrowserGateway;
//using CFW.AppHost.Infrastructures.IXBrowserGateway.Models;
//using CFW.DynamicApi;

//namespace CFW.AppHost.Features.BrowserProfiles;

//[ApiOperation("browser-profiles", RouteName = "open")]
//public class BrowserProfilesOpen : IRequestHandler<OpenProfileRequest, OpenProfileResponse>
//{
//    private readonly IProfileGateway _profileGateway;

//    public BrowserProfilesOpen(IProfileGateway profileGateway)
//    {
//        _profileGateway = profileGateway;
//    }

//    public async Task<IResult<OpenProfileResponse>> Handle(RequestModel<OpenProfileRequest> request, CancellationToken cancellationToken)
//    {
//        var result = await _profileGateway.OpenProfileAsync(request.Model);
//        if (result.Error?.Code != 0)
//        {
//            return result.Failed(result.Error!.ToJsonString());
//        }

//        return result.Success();
//    }
//}
