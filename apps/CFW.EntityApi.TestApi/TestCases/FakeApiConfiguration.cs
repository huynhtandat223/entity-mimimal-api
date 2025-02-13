using CFW.EntityApi.Intefaces;
using CFW.EntityApi.Models.Builders;

namespace CFW.EntityApi.TestApi.TestCases;

[EntityConfiguration]
public class FakeApiConfiguration : IEntityApiConfiguration
{
    private readonly Action<EntityApiContextBuilder> _setupAction;

    public FakeApiConfiguration(Action<EntityApiContextBuilder> setupAction)
    {
        _setupAction = setupAction;
    }

    public void Configure(EntityApiContextBuilder builder)
    {
        _setupAction?.Invoke(builder);
    }
}
