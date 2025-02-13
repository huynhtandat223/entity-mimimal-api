using CFW.EntityApi.Models.Builders;

namespace CFW.EntityApi.Intefaces;

public interface IEntityApiConfiguration
{
    void Configure(EntityApiContextBuilder builder);
}
