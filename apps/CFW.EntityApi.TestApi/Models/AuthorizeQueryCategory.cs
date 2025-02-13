using CFW.Core.Entities;
using CFW.EntityApi.Models;

namespace CFW.EntityApi.TestApi.Models;

[Entity("authorize-query-categories")]
[EntityAuthorize(ApplyMethods = [ApiMethod.Query])]
public class AuthorizeQueryCategory : IEntity<Guid>
{
    public Guid Id { get; set; }

    public string Name { get; set; } = string.Empty;
}
