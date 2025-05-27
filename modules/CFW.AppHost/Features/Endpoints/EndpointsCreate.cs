using CFW.AppHost.Features.Endpoints.Configurations;
using CFW.AppHost.Features.Shared;
using CFW.Core.Builders.RuntimeTypeBuilders;
using CFW.DynamicApi;
using CFW.DynamicApi.Entensions;
using Microsoft.Extensions.Options;
using System.Reflection;
using System.Reflection.Emit;
using Endpoint = CFW.AppHost.Features.Endpoints.Models.Endpoint;

namespace CFW.AppHost.Features.Endpoints;

[ApiOperation("endpoints")]
public class EndpointsCreate : IRequestHandler<Endpoint, Endpoint>
{
    private readonly RuntimeAsmConfig _runtimeAsmConfig;

    public EndpointsCreate(IOptions<RuntimeAsmConfig> runtimeAsmConfig)
    {
        _runtimeAsmConfig = runtimeAsmConfig.Value;
    }

    public async Task<IResult<Endpoint>> Handle(RequestModel<Endpoint> request, CancellationToken cancellationToken)
    {
        var endpoint = request.Model;
        if (endpoint.RuntimeEntityDefinition is null)
        {
            throw new NotImplementedException("RuntimeEntityDefinition is not implemented yet.");
        }

        var typeDef = endpoint.RuntimeEntityDefinition;
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

        var result = await request.CreateEntity<Endpoint, AppDbContext>();
        return result.Created();
    }
}