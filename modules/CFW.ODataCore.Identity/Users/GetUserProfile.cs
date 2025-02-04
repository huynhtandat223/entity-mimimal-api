using CFW.Core.Results;
using CFW.ODataCore.Attributes;
using CFW.ODataCore.Identity.Users.Models;
using CFW.ODataCore.Intefaces;
using CFW.ODataCore.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace CFW.ODataCore.Identity.Users;

public class GetUserProfile
{
    public record Request { }

    public class Response
    {
        public string? UserName { get; set; }

        public string? Email { get; set; }

        public IEnumerable<ResponseTenantRole> TenantRoles { get; set; }
            = Array.Empty<ResponseTenantRole>();
    }

    public class ResponseTenantRole
    {
        public string? Name { get; set; }

        public Guid TenantId { get; set; }
    }

    [UnboundAction("me", ActionMethod = ApiMethod.Get)]
    public class Handler : IOperationHandler<Request, Response>
    {
        private readonly CoreIdentityDbContext _db;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public Handler(CoreIdentityDbContext db
            , IHttpContextAccessor httpContextAccessor
            , UserManager<ApplicationUser> userManager)
        {
            _db = db;
            _userManager = userManager;
            _httpContextAccessor = httpContextAccessor;
        }

        public async Task<Result<Response>> Handle(Request _, CancellationToken cancellationToken)
        {
            var result = new Response();
            var claimsPrincipal = _httpContextAccessor.HttpContext?.User;
            if (claimsPrincipal is null)
                return result.Notfound();

            var userName = claimsPrincipal.Identity?.Name;
            var currentUser = await _userManager.Users
                .Include(x => x.TenantRoles)
                .FirstOrDefaultAsync(x => x.UserName == userName, cancellationToken);

            if (currentUser is null)
                return result.Notfound();

            var roles = await _userManager
                .GetRolesAsync(currentUser);

            result = new Response
            {
                UserName = currentUser!.UserName,
                Email = currentUser.Email,
                TenantRoles = currentUser.TenantRoles.Select(x => new ResponseTenantRole
                {
                    Name = x.Name,
                    TenantId = x.TenantId
                })
            };

            return result.Ok();
        }
    }
}
