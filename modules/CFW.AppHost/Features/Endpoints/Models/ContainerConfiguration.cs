using CFW.Core.Entities;

namespace CFW.AppHost.Features.Endpoints.Models;

public class ContainerConfiguration : IEntity<Guid>
{
    public Guid Id { get; set; }

    public string RoutePrefix { get; set; } = string.Empty;

    public int? DefaultPageSize { get; set; }

    public ICollection<Endpoint>? Endpoints { get; set; }
}
