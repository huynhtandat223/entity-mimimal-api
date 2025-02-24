using CFW.EntityApi.Models.Builders;
using CFW.EntityApi.TestApi.Features.Entities.ViewModels;
using CFW.EntityApi.TestApi.Infrastructures.DbContexts;

namespace CFW.EntityApi.TestApi.Features.Entities;

public class ApiConfiguration : IEntityApiConfiguration<EntityViewModel>
{
    public Task Configure(EntityApiConfigurationBuilder<EntityViewModel> builder)
    {
        builder
            .UseName("entities")
            .UseQuery<AppDbContext>(db => db.Model.GetEntityTypes().Select(x => new EntityViewModel
            {
                Id = x.ClrType.FullName!,
                Name = x.ClrType.Name

            }).ToList().AsQueryable());

        return Task.CompletedTask;
    }
}
