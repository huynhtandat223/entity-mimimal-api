using CFW.AppHost.Features.Databases.Endpoints;
using CFW.AppHost.Infrastructures.DbContextExtensions.Models;
using Shouldly;
using System.Net.Http.Json;
using Xunit.Abstractions;
using Xunit.Extensions.AssemblyFixture;

namespace CFW.AppHost.Tests.TestCases.Databases;
public class DatabasesListTableTests : BaseTests, IAssemblyFixture<AppFactory>
{
    public DatabasesListTableTests(ITestOutputHelper testOutputHelper, AppFactory factory) : base(testOutputHelper, factory)
    {
    }

    [Fact]
    public async Task RequestShouldSuccess()
    {
        // Arrange
        var client = _factory.CreateClient();

        //Act
        var request = new
        {
            //ConnectionString = "Server=localhost\\SQLEXPRESS;\r\nDatabase=cirrusvm;\r\nUser ID=sa;\r\nPassword=123456;\r\nEncrypt=True;\r\nTrustServerCertificate=True;\r\nMultipleActiveResultSets=True;\r\n",
            DatabaseProvider = DatabaseProvider.MSSQL
        };

        var responseMsg = await client.PostAsJsonAsync(
            $"{Constants.DefaultTestingRoutePrefix}databases/tables", request);

        // Assert
        responseMsg.IsSuccessStatusCode.ShouldBeTrue();
        var response = await responseMsg.Content.ReadFromJsonAsync<DatabasesListTable.Response>();

    }
}
