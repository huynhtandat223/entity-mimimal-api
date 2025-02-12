using CFW.EntityApi.Registrators;

namespace CFW.EntityApi.Models.Builders;

public interface IEntityMemberApiBuilder
{
    void Build(ContainerRegistrationContext containerRegistrationContext);
}
