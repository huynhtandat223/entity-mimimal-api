using CFW.Core.Entities;

namespace CFW.EntityMinimalApi.Testings.Models;

[Entity("authorize-categories")]
[EntityAuthorize]
public class AuthorizeCategory : IEntity<Guid>
{
    public Guid Id { get; set; }

    public string Name { get; set; } = string.Empty;
}
