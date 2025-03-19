using CFW.EntityApi.Models.Builders;
using CFW.ODataCore.Features.PageLayers.Models;
using Microsoft.EntityFrameworkCore;

namespace CFW.ODataCore.Features.PageLayers;

public class ApiConfiguration : IEntityApiConfiguration<PageLayer>
{
    public Task Configure(EntityApiConfigurationBuilder<PageLayer> builder)
    {
        builder.UseName("page-layers")
            .UseQuery<AppDbContext>(db => db.Set<PageLayer>().AsNoTracking());


        return Task.CompletedTask;
    }
}
