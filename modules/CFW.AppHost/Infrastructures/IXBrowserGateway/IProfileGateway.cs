using CFW.AppHost.Infrastructures.IXBrowserGateway.Models;
using Refit;

namespace CFW.AppHost.Infrastructures.IXBrowserGateway;

public interface IProfileGateway
{
    [Post("/profile-list")]
    Task<ProfileListResponse> GetProfileListAsync([Body] ProfileListRequest request);
}
