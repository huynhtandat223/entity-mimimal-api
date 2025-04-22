using CFW.Core.Builders.RuntimeTypeBuilders;
using CFW.Core.Dependencies;
using CFW.Core.Results;
using Microsoft.Extensions.Options;
using System.Reflection;
using System.Reflection.Emit;

namespace CFW.ODataCore.Features.Endpoints.Services;

public class EndpointService : IScopedService
{
    private readonly RuntimeAsmConfig _runtimeAsmConfig;

    public EndpointService(IOptions<RuntimeAsmConfig> options)
    {
        _runtimeAsmConfig = options.Value;
    }

    public async Task<Result> CreateEndpoint(Models.Endpoint endpoint)
    {
        if (endpoint.RuntimeEntityDefinition is null)
        {
            throw new NotImplementedException("RuntimeEntityDefinition is not implemented yet.");
        }

        var typeDef = endpoint.RuntimeEntityDefinition;
        var fullTypeName = string.Join('.', typeDef.Namespace, typeDef.Name);

        PersistedAssemblyBuilder assemblyBuilder = new PersistedAssemblyBuilder(new AssemblyName(typeDef.Namespace)
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

        return this.Success();
    }
}
