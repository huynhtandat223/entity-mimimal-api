using CFW.EntityApi.Models.Builders;
using CFW.EntityApi.TestApi.Features.Entities.ViewModels;

namespace CFW.EntityApi.TestApi.Features.Entities;

public class ApiConfiguration : IEntityApiConfiguration<EntityViewModel>
{
    public Task Configure(EntityApiConfigurationBuilder<EntityViewModel> builder)
    {
        builder
            .UseName("entities")
            .UseQuery<DefaultDbContext>(db => db.Model.GetEntityTypes().Select(x => new
            {
                x.Name,
                x.ClrType.FullName,
                Properties = x.GetProperties().Select(p => new
                {
                    p.Name,
                    p.ClrType.FullName
                })
            }).AsQueryable());

        return Task.CompletedTask;
    }
}
