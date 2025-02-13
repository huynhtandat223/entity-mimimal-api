using CFW.EntityApi.Intefaces;

namespace CFW.EntityApi.TestApi.Features.Categories;

public class CategoriesGetPingPong
{
    public record RequestPing
    {
        public Guid Id { get; set; }

        public string Name { get; set; } = string.Empty;
    }

    public record ResponsePong : RequestPing
    {
    }

    [EntityAction($"categories/getPingPong", Method = EntityApi.Models.ApiMethod.Get)]
    public class Handler : IOperationHandler<RequestPing, ResponsePong>
    {
        private readonly List<object> _requestObjects;

        public Handler(List<object> requestObjects)
        {
            _requestObjects = requestObjects;
        }

        public Task<Result<ResponsePong>> Handle(RequestPing request, CancellationToken cancellationToken)
        {
            var result = new ResponsePong
            {
                Id = request.Id,
                Name = request.Name
            };

            _requestObjects.Add(result);

            return Task.FromResult(result.Success());
        }
    }
}
