using CFW.EntityApi.Intefaces;
using CFW.EntityMinimalApi.Testings.Models;

namespace CFW.EntityMinimalApi.Testings.Features.Categories;

public class CategoriesPingPongWithNonKeyNoResponseDataAttr
{
    public record RequestPing
    {
        public Guid Id { get; set; }

        public string Name { get; set; } = string.Empty;
    }

    [EntityAction<Category>(nameof(CategoriesPingPongWithNonKeyNoResponseDataAttr), EntityName = "categories")]
    public class Handler : IOperationHandler<RequestPing>
    {
        public Task<Result> Handle(RequestPing request, CancellationToken cancellationToken)
        {
            var result = this.Ok() as Result;
            return Task.FromResult(result);
        }
    }
}
