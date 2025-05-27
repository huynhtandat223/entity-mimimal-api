using CFW.Core.Dependencies;

namespace CFW.AppHost.Features.Endpoints.Configurations;

[SectionConfig<RuntimeAsmConfig>]
public class RuntimeAsmConfig
{
    public string RuntimeEntitiesDir { set; get; } = string.Empty;

    public string GetRuntimeEntitiesDirOrDefault()
    {
        var result = RuntimeEntitiesDir.IsNotNullOrNotWhiteSpace()
            ? RuntimeEntitiesDir
            : Path.Combine(Environment.CurrentDirectory, "RuntimeEntties");

        if (!Directory.Exists(result))
        {
            Directory.CreateDirectory(result);
        }

        return result;
    }
}
