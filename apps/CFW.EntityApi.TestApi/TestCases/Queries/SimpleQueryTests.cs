using CFW.EntityApi.Models;
using CFW.EntityApi.TestApi.Infrastructures.DbContexts;

namespace CFW.EntityApi.TestApi.TestCases.Queries;

public class SimpleQueryTests : BaseTests, IAssemblyFixture<AppFactory>
{
    public SimpleQueryTests(ITestOutputHelper testOutputHelper, AppFactory factory) : base(testOutputHelper, factory)
    {
    }

    [Theory]
    [GenericData(nameof(GetTestData), Constants.DefaultODataRoutePrefix)]
    public async Task Query_NoParameters_Success<T>(TestData<T> testData)
        where T : class
    {
        // Arrange
        var factory = SetupEntityApi(testData.RoutePrefix, testData.DataProvider);

        var baseUrl = testData.Url;
        var client = factory.CreateClient();
        var db = factory.GetScopedService<AppDbContext>();
        var complexProps = db.Set<T>().GetComplexAndNavigationPropertyNames();

        var expected = await SeedDataIfNotAnyRecords<T>(6, db);

        // Act
        var actual = await client.GetFromJsonAsync<ODataQueryResult<T>>(baseUrl, DefaultJsonSeriallizerOptions);

        // Assert
        actual.Should().NotBeNull();
        actual!.TotalCount.Should().BeNull();

        actual.Value.Should()
            .BeEquivalentTo(expected, o => o.Excluding(e => complexProps.Contains(e.Name)));
    }

    [Theory]
    [GenericData(nameof(GetViewModelTestData), Constants.DefaultODataRoutePrefix)]
    public async Task CustomViewModel_Query_NoParameters_Success<T>(TestData<T> testData)
        where T : class
    {
        // Arrange
        var factory = SetupEntityApi(testData.RoutePrefix, testData.DataProvider);

        var baseUrl = testData.Url;
        var client = factory.CreateClient();

        // Act
        var actual = await client.GetFromJsonAsync<ODataQueryResult<T>>(baseUrl, DefaultJsonSeriallizerOptions);

        // Assert
        actual.Should().NotBeNull();
        actual!.TotalCount.Should().BeNull();
    }
}
