using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace CFW.Core.Dependencies;

public abstract class SectionConfigAttribute : Attribute
{
    public abstract void Configure(IServiceCollection services, IConfiguration configuration);
}

[AttributeUsage(AttributeTargets.Class)]
public class SectionConfigAttribute<T> : SectionConfigAttribute
    where T : class
{
    public string? Section { get; set; }

    public override void Configure(IServiceCollection services, IConfiguration configuration)
    {
        var sectionName = Section ?? typeof(T).Name;

        services.Configure<T>(configuration.GetSection(sectionName));

    }
}
