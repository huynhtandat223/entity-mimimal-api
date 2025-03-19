using CFW.EntityApi.Models;

namespace CFW.EntityApi.Registrators;

public class ContainerRegistrationContext
{
    private readonly ITypesResolver _typeResolver;

    public ContainerRegistrationContext(IServiceProvider serviceProvider, ContainerConfiguration containerConfiguration
        , ITypesResolver typeResolver)
    {
        RootServiceProvider = serviceProvider;
        ContainerConfiguration = containerConfiguration;
        _typeResolver = typeResolver;
    }

    internal RouteGroupBuilder ContainerGroupRoute { get; set; } = null!;

    public ContainerConfiguration ContainerConfiguration { get; set; } = null!;

    public IServiceProvider RootServiceProvider { get; }

    public ITypesResolver TypeResolver => _typeResolver;
}
