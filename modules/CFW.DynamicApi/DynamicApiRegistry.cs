using CFW.DynamicApi.Buiders;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace CFW.DynamicApi;

public class DynamicApiRegistry
{
    private readonly List<DynamicEntityGroupBuilder> _apiGroups = new();
    private readonly List<ApiOperationAttribute> _apiOperationAttributes;

    private readonly IServiceProvider _serviceProvider;

    public DynamicApiRegistry(List<ApiOperationAttribute> apiOperationAttributes
        , IServiceProvider serviceProvider)
    {
        _apiOperationAttributes = apiOperationAttributes;
        _serviceProvider = serviceProvider;

        RegisterAttributeApiGroup();
    }

    internal void RegisterApiGroup(DynamicEntityGroupBuilder apiGroup)
    {
        _apiGroups.Add(apiGroup);
    }

    private void RegisterAttributeApiGroup()
    {
        var groups = _apiOperationAttributes.GroupBy(x => x.RouteGroup).ToList();
        foreach (var group in groups)
        {
            var apiGroup = new DynamicEntityGroupBuilder
            {
                RouteName = group.Key,
            };

            foreach (var operation in group)
            {
                var targetType = operation.TargetType;
                var operationInteface = operation.TargetType!.GetInterfaces()
                    .FirstOrDefault(x => x.IsGenericType && x.GetGenericTypeDefinition() == typeof(IRequestHandler<,>));
                var requestType = operationInteface?.GetGenericArguments()[0];
                var responseType = operationInteface?.GetGenericArguments()[1];

                var apiOprationType = typeof(ApiOperation<,>).MakeGenericType(requestType!, responseType!);
                var apiOpration = ActivatorUtilities
                    .CreateInstance(_serviceProvider, apiOprationType, operation.TargetType) as DynamicApiOperation;
                apiOpration!.HttpMethod = operation.HttpMethod.ToString();
                apiOpration.Route = operation.RouteName!; //use ApiOperation attr it can null
                apiOpration.EntityGroup = apiGroup;
                apiGroup.Operations.Add(apiOpration);
            }

            RegisterApiGroup(apiGroup);
        }
    }

    internal void MapApi(WebApplication app, ContainerConfiguration containerConfig)
    {
        var containerGroupBuilder = app
            .MapGroup(containerConfig.RoutePrefix)
            .WithGroupName(containerConfig.RoutePrefix);

        foreach (var apiGroup in _apiGroups)
        {
            var group = containerGroupBuilder
                .MapGroup(apiGroup.RouteName)
                .WithGroupName(apiGroup.RouteName);

            if (!apiGroup.IsAllowAnonymous)
            {
                group = group.RequireAuthorization(apiGroup.AuthorizeDatas.ToArray());
            }

            foreach (var operation in apiGroup.Operations)
            {
                var routeHandlerBuilder = operation.MapApi(group);
                routeHandlerBuilder
                    .WithTags(apiGroup.RouteName);
            }
        }
    }
}
