using CFW.EntityApi.Intefaces;
using CFW.EntityApi.Models;

namespace CFW.EntityApi.TestApi.Features.Categories;

public class CategoriesPingPongPutMethodNonKey
{
    public record RequestPing
    {
        public Guid Id { get; set; }

        public string Name { get; set; } = string.Empty;
    }

    public record ResponsePong
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
    }

    [EntityAction<Category>(nameof(CategoriesPingPongPutMethodNonKey)
        , HttpMethod = ApiMethod.Put
        , EntityName = "categories")]
    public class Handler : IOperationHandler<RequestPing, ResponsePong>
    {
        public Task<Result<ResponsePong>> Handle(RequestPing request, CancellationToken cancellationToken)
        {
            var result = new ResponsePong
            {
                Id = request.Id,
                Name = request.Name
            }.Ok();

            return Task.FromResult(result);
        }
    }
}