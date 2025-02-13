using CFW.EntityApi.Registrators;

namespace CFW.EntityApi.Models.Builders;

public interface IApiFeature
{
    Task Register(ContainerRegistrationContext containerRegistrationContext);
}
