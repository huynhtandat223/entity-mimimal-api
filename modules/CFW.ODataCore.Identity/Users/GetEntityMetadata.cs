using CFW.Core.Results;
using CFW.ODataCore.Attributes;
using CFW.ODataCore.Intefaces;
using CFW.ODataCore.Models;
using CFW.ODataCore.Models.Metadata;

namespace CFW.ODataCore.Identity.Users;

public class GetEntityMetadata
{
    public class Request : IRequestMetadata
    {
        public MetadataAction Metadata { get; set; } = default!;
    }

    public class Response
    {
        public IEnumerable<ResponseEntity> MetadataEntities { get; set; } = Array.Empty<ResponseEntity>();
    }

    public class ResponseEntity
    {
        public string Name { get; set; } = string.Empty;

        public IEnumerable<ApiMethod> Methods { get; set; } = Array.Empty<ApiMethod>();
    }

    [UnboundAction("entityMetadata", ActionMethod = ApiMethod.Get)]
    public class Handler : IOperationHandler<Request, Response>
    {
        private readonly IEnumerable<EntityMimimalApiOptions> _entityMimimalApiOptions;

        public Handler(IEnumerable<EntityMimimalApiOptions> entityMimimalApiOptions)
        {
            _entityMimimalApiOptions = entityMimimalApiOptions;
        }

        public async Task<Result<Response>> Handle(Request request, CancellationToken cancellationToken)
        {
            var result = new Response();
            var options = _entityMimimalApiOptions
                .SingleOrDefault(x => x.DefaultRoutePrefix == request.Metadata.RoutePrefix);

            if (options is null)
                return result.Notfound($"Option with route prefix {request.Metadata.RoutePrefix} not found");

            var container = options.MetadataContainer;
            result.MetadataEntities = container.MetadataEntities
                .Select(x => new ResponseEntity
                {
                    Name = x.Name,
                    Methods = x.Methods
                });

            return result.Ok();
        }
    }
}
