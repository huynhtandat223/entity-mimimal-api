using CFW.AppHost.Infrastructures.IXBrowserGateway.Models;
using Refit;

namespace CFW.AppHost.Infrastructures.IXBrowserGateway;

public interface IProfileGateway
{
    [Post("/profile-list")]
    Task<ProfileListResponse> GetProfileListAsync([Body] ProfileListRequest request);

    [Post("/profile-create")]
    Task<CreateProfileResponse> CreateProfileAsync([Body] CreateProfileRequest request);

    [Post("/profile-copy")]
    Task<CopyProfileResponse> CopyProfileAsync([Body] CopyProfileRequest request);

    [Post("/profile-open")]
    Task<OpenProfileResponse> OpenProfileAsync([Body] OpenProfileRequest request);

    [Post("/profile-opened-list")]
    Task<GetOpenedProfilesResponse> GetOpenedProfilesAsync([Body] GetOpenedProfilesRequest emptyBody);
}
