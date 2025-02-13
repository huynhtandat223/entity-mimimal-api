namespace CFW.EntityApi.TestApi.Infrastructures.UnitTests;

public static class WebApplictionFactoryExtensions
{
    public static T GetScopedService<T>(this WebApplicationFactory<Program> factory)
        where T : class
    {
        return factory.Services.CreateScope().ServiceProvider.GetRequiredService<T>();
    }
}
