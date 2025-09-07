using CFW.AppHost.Features.Endpoints.Models;
using CFW.AppHost.Infrastructures.RunTimeDevelopments.Models;
using Shouldly;
using System.Net.Http.Json;
using Xunit.Abstractions;
using Xunit.Extensions.AssemblyFixture;

namespace CFW.AppHost.Tests.TestCases.Endpoints;
public class EndpointsCreateTests : BaseTests, IAssemblyFixture<AppFactory>
{
    public EndpointsCreateTests(ITestOutputHelper testOutputHelper, AppFactory factory)
        : base(testOutputHelper, factory)
    {

    }

    [Fact]
    public async Task Test()
    {
        // Arrange
        var client = _factory.CreateClient();

        // Act
        var request = new Endpoint
        {
            Authorization = null,
            Description = "description",
            Id = Guid.NewGuid(),
            IsAuthenticationRequired = false,
            Method = Features.Endpoints.Models.HttpMethod.GET,
            ODataOptions = null,
            Path = "/simple-api",
            ContainerConfiguration = new ContainerConfiguration
            {
                DefaultPageSize = 10,
                RoutePrefix = $"{Constants.DefaultTestingRoutePrefix}"
            },
            RuntimeEntityDefinition = new RuntimeEntityDefinition
            {
                Name = "TestEntity",
                Namespace = "RuntimeApis",
                Properties = new List<RuntimeEntityPropertyDefinition>
                {
                    new RuntimeEntityPropertyDefinition
                    {
                        Id = Guid.NewGuid(),
                        IsKey = true,
                        Name = "Id",
                        Type = typeof(Guid).AssemblyQualifiedName!
                    },
                    new RuntimeEntityPropertyDefinition
                    {
                        Id = Guid.NewGuid(),
                        Name = "Name",
                        Type = typeof(string).AssemblyQualifiedName!,
                        IsRequired = true,
                    }
                }
            }
        };

        var response = await client.PostAsJsonAsync($"{Constants.DefaultTestingRoutePrefix}endpoints", request);

        // Assert
        response.IsSuccessStatusCode.ShouldBeTrue();

        var newApiEndpoint = $"{Constants.DefaultTestingRoutePrefix}simple-api";
        response = await client.GetAsync(newApiEndpoint);
        response.IsSuccessStatusCode.ShouldBeTrue();
    }
}
