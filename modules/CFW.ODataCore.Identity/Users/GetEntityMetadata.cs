using CFW.Core.Results;
using CFW.ODataCore.Attributes;
using CFW.ODataCore.Intefaces;
using CFW.ODataCore.Models;

namespace CFW.ODataCore.Identity.Users;

public class GetEntityMetadata
{
    public record Request { }

    public class Response
    {
        public IEnumerable<ResponseContainer> Containers { get; set; } = Array.Empty<ResponseContainer>();
    }

    public class ResponseContainer
    {
        public string RoutePrefix { get; set; } = string.Empty;
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
        private readonly EntityMimimalApiOptions _entityMimimalApiOptions;
        public Handler(EntityMimimalApiOptions entityMimimalApiOptions)
        {
            _entityMimimalApiOptions = entityMimimalApiOptions;
        }

        public async Task<Result<Response>> Handle(Request _, CancellationToken cancellationToken)
        {
            var result = new Response();

            result.Containers = _entityMimimalApiOptions.Containters
                .Select(x => new ResponseContainer
                {
                    RoutePrefix = x.RoutePrefix,
                    MetadataEntities = x.MetadataEntities
                        .Select(y => new ResponseEntity
                        {
                            Name = y.Name,
                            Methods = y.Methods
                        })
                });

            return result.Ok();
        }
    }
}
