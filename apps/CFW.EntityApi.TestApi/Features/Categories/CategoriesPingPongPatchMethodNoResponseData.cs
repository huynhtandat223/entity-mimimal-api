using CFW.EntityApi.Intefaces;
using CFW.EntityApi.Models;

namespace CFW.EntityApi.TestApi.Features.Categories;

public class CategoriesPingPongPatchMethodNoResponseData
{
    public record RequestPing
    {
        public Guid Id { get; set; }

        public string Name { get; set; } = string.Empty;
    }

    [EntityAction<Category>(nameof(CategoriesPingPongPatchMethodNoResponseData)
        , HttpMethod = ApiMethod.Patch
        , EntityName = "categories")]
    public class Handler : IOperationHandler<RequestPing>
    {
        public Task<Result> Handle(RequestPing request, CancellationToken cancellationToken)
        {
            var result = this.Ok() as Result;
            return Task.FromResult(result);
        }
    }
}
