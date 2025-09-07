using CFW.AppHost.Features.Endpoints.Endpoints;
using CFW.AppHost.Features.Endpoints.Models;
using Shouldly;
using System.Net.Http.Json;
using Xunit.Abstractions;
using Xunit.Extensions.AssemblyFixture;

namespace CFW.AppHost.Tests.TestCases.Endpoints;
public class EndpointsCreateFromTableTests : BaseTests, IAssemblyFixture<AppFactory>
{
    public EndpointsCreateFromTableTests(ITestOutputHelper testOutputHelper, AppFactory factory)
        : base(testOutputHelper, factory)
    {

    }

    [Fact]
    public async Task Test()
    {
        // Arrange
        var client = _factory.CreateClient();

        // Act
        var request = new EndpointsCreateFromTable.Request
        {
            Authorization = null,
            Description = "description",
            Id = Guid.NewGuid(),
            IsAuthenticationRequired = false,
            Method = Features.Endpoints.Models.HttpMethod.GET,
            ODataOptions = null,
            Path = "/simple-api-from-table",
            ContainerConfiguration = new ContainerConfiguration
            {
                DefaultPageSize = 10,
                RoutePrefix = $"{Constants.DefaultTestingRoutePrefix}"
            },
            TableName = "AwsAccount",
            DatabaseConfiguration = new Infrastructures.DbContextExtensions.Models.DatabaseConfiguration
            {
                DatabaseProvider = Infrastructures.DbContextExtensions.Models.DatabaseProvider.MSSQL,
                MSSQL = new Infrastructures.DbContextExtensions.Models.MSSQLConfig
                {
                    Database = "cirrusvm",
                    Server = "localhost\\SQLEXPRESS",
                    Username = "sa",
                    Password = "123456",
                    Encrypt = true,
                    TrustServerCertificate = true,
                    MultipleActiveResultSets = true,
                }
            },
            //Server=localhost\\SQLEXPRESS;Database=cirrusvm;User ID=sa;Password=123456;Encrypt=True;TrustServerCertificate=True;MultipleActiveResultSets=True;
        };

        var response = await client.PostAsJsonAsync($"{Constants.DefaultTestingRoutePrefix}endpoints/tables", request);

        // Assert
        response.IsSuccessStatusCode.ShouldBeTrue();

        var newApiEndpoint = $"{Constants.DefaultTestingRoutePrefix}simple-api-from-table";
        response = await client.GetAsync(newApiEndpoint);
        response.IsSuccessStatusCode.ShouldBeTrue();
    }
}
