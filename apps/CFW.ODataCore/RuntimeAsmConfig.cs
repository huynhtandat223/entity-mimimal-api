using CFW.Core.Utils;

namespace CFW.ODataCore;

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
