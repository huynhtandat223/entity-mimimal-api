using CFW.Core.EfCoreExtensions;
using CFW.EntityApi.Models;
using CFW.EntityApi.TestApi.Infrastructures.DbContexts;

namespace CFW.EntityApi.TestApi.TestCases.Queries;

public class QueryFilterTests : BaseTests, IAssemblyFixture<AppFactory>
{
    public QueryFilterTests(ITestOutputHelper testOutputHelper, AppFactory factory) : base(testOutputHelper, factory)
    {
    }

    [Theory]
    [GenericData(nameof(GetTestData), Constants.DefaultODataRoutePrefix)]
    [CombineFrom([0], nameof(TestUtils.GetEntityType))]
    [CombineFrom([1], nameof(TestUtils.PickRandomProperties), AdditionalArguments = [2])]
    public async Task QueryOrderBySingleScalarProperty_Success<T>(TestData<T> testData
        , Type _, string[] properties)
        where T : class
    {
        // Arrange
        var factory = SetupEntityApi(testData.RoutePrefix, testData.DataProvider);

        var baseUrl = testData.Url;
        var client = factory.CreateClient();
        var db = factory.GetScopedService<AppDbContext>();
        var scalaProperties = db.Set<T>().GetScalarProperties();
        properties = properties.Intersect(scalaProperties).ToArray();

        if (properties.Length == 0)
        {
            _testOutputHelper.WriteLine("No scalar properties found for the entity");
            return;
        }

        var setupData = await SeedDataIfNotAnyRecords<T>(6, db);
        var randomPropertyName = properties.Random();
        var randomValue = setupData
            .Random()
            .GetPropertyValue(randomPropertyName);

        var filterValue = randomValue!.FormatOdataFilter();

        // Act
        var actual = await client.GetFromJsonAsync<ODataQueryResult<T>>(
            $"{baseUrl}?$filter={randomPropertyName} eq {filterValue}", DefaultJsonSeriallizerOptions);

        // Assert
        actual.Should().NotBeNull();
        var actualValues = actual!.Value!.Select(x => x.GetPropertyValue(randomPropertyName)).ToList()!;
        actualValues.Should().Contain(randomValue!);
    }
}
