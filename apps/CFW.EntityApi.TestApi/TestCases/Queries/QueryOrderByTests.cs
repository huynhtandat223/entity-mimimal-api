using CFW.Core.EfCoreExtensions;
using CFW.EntityApi.Models;
using CFW.EntityApi.TestApi.Infrastructures.DbContexts;

namespace CFW.EntityApi.TestApi.TestCases.Queries;

public class QueryOrderByTests : BaseTests, IAssemblyFixture<AppFactory>
{
    public QueryOrderByTests(ITestOutputHelper testOutputHelper, AppFactory factory) : base(testOutputHelper, factory)
    {
    }


    [Theory]
    [GenericData(nameof(GetTestData), Constants.DefaultODataRoutePrefix)]
    [CombineFrom([0], nameof(TestUtils.GetEntityType))]
    [CombineFrom([1], nameof(TestUtils.PickRandomProperties), AdditionalArguments = [2])]
    [CombineRandomValue(typeof(bool), ValueCount = 2)]
    public async Task QueryOrderBySingleScalarProperty_Success<T>(TestData<T> testData
        , Type _, string[] properties, bool isAsc)
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

        var expected = await SeedDataIfNotAnyRecords<T>(6, db);
        var randomProperty = properties.Random();

        // Act
        var orderByQuery = $"?$orderby={randomProperty}{(!isAsc ? " desc" : string.Empty)}";

        var actual = await client.GetFromJsonAsync<ODataQueryResult<T>>($"{baseUrl}{orderByQuery}"
            , DefaultJsonSeriallizerOptions);

        // Assert
        actual.Should().NotBeNull();

        var actualValues = actual!.Value
            .OrderByProperty(testData.IdPropertyName)
            .Select([randomProperty]);

        var expectedValues = expected
            .OrderByProperty(randomProperty, isAsc)
            .OrderByProperty(testData.IdPropertyName)
            .Select([randomProperty]);

        actualValues.Should().BeEquivalentTo(expectedValues, o => o
            .WithStrictOrdering());
    }

}

