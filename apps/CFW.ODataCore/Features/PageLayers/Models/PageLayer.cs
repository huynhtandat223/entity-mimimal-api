using CFW.Core.Entities;

namespace CFW.ODataCore.Features.PageLayers.Models;

public class PageLayer : IEntity<string>
{
    public string Id { set; get; } = string.Empty;

    public string Type { set; get; } = string.Empty;

    public string Name { set; get; } = string.Empty;

    public string? Route { set; get; }

    public string? Props { set; get; }

    public string? Children { set; get; }
}
