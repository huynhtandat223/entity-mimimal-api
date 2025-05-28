using CFW.AppHost.Features.Endpoints.Configurations;
using CFW.AppHost.Features.Endpoints.Models;
using CFW.Core.Builders.RuntimeTypeBuilders;
using CFW.Core.Dependencies;
using Microsoft.Extensions.Options;
using System.Reflection;
using System.Reflection.Emit;

namespace CFW.AppHost.Features.Endpoints.Infrastructures;

public class RuntimeTypeRegistry : ISingletonService
{
    private readonly RuntimeAsmConfig _runtimeAsmConfig;
    private readonly ILogger _logger;

    private List<Type> _runtimeTypes = new List<Type>();

    public RuntimeTypeRegistry(ILogger<RuntimeTypeRegistry> logger,
        IOptions<RuntimeAsmConfig> runtimeAsmConfig)
    {
        _runtimeAsmConfig = runtimeAsmConfig.Value;
        _logger = logger;
    }

    public IReadOnlyCollection<Type> RuntimeTypes => _runtimeTypes.AsReadOnly();

    public async Task<IEnumerable<Type>> LoadRuntimeTypes(IEnumerable<Models.RuntimeEntityDefinition> runtimeEntityDefinitions)
    {
        if (!runtimeEntityDefinitions.Any())
            return _runtimeTypes;

        var runtimeDir = _runtimeAsmConfig.GetRuntimeEntitiesDirOrDefault();

        foreach (var file in Directory.GetFiles(runtimeDir, "*.dll"))
        {
            try
            {
                // Load without locking the file
                var assemblyBytes = File.ReadAllBytes(file);
                var assembly = Assembly.Load(assemblyBytes);

                foreach (var type in assembly.GetTypes())
                {
                    // Optional: only add entity-related types
                    if (runtimeEntityDefinitions.Any(e => type.FullName == $"{e.Namespace}.{e.Name}"))
                        _runtimeTypes.Add(type);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to load assembly {File}: {Message}", file, ex.Message);
            }
        }

        return await Task.FromResult(_runtimeTypes);
    }

    public Type CreateTypeAddLoad(RuntimeEntityDefinition typeDef)
    {
        var fullTypeName = string.Join('.', typeDef.Namespace, typeDef.Name);

        var assemblyBuilder = new PersistedAssemblyBuilder(new AssemblyName(typeDef.Namespace)
            , typeof(object).Assembly);
        var moduleBuilder = assemblyBuilder.DefineDynamicModule("MainModule");

        var propDefs = typeDef.Properties.Select(x => new RuntimePropertyDefinition
        {
            Name = x.Name,
            Type = Type.GetType(x.Type)!,
            IsKey = x.IsKey,
            IsRequired = x.IsRequired
        });
        var typeName = string.Join('.', typeDef.Namespace, typeDef.Name);
        var type = RuntimeTypeBuilder.CreateType(
            new RuntimeTypeDefinition
            {
                TypeName = typeName,
                ModuleBuilder = moduleBuilder
            }, propDefs);

        var fullPath = Path.Combine(_runtimeAsmConfig.GetRuntimeEntitiesDirOrDefault(), fullTypeName + ".dll");
        assemblyBuilder.Save(fullPath);

        var loadedType = Assembly.LoadFrom(fullPath).GetType(fullTypeName);

        _runtimeTypes.Add(loadedType);
        return loadedType;
    }
}
