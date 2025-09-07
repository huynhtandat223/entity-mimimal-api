using CFW.DynamicApi;

namespace CFW.AppHost.Features.Endpoints.Endpoints;

[ApiOperation("endpoints/tables")]
public class EndpointsCreateFromTable
{
    public class Request
    {
        public string TableName { get; set; } = string.Empty;
    }

    public class Handler : IRequestHandler<Request, Endpoint>
    {
        public Task<IResult<Endpoint>> Handle(RequestModel<Request> request, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }
    }
}
