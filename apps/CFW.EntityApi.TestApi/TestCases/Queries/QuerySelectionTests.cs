using CFW.Core.EfCoreExtensions;
using CFW.EntityApi.Models;
using CFW.EntityApi.TestApi.Infrastructures.DbContexts;

namespace CFW.EntityApi.TestApi.TestCases.Queries;

public class QuerySelectionTests : BaseTests, IAssemblyFixture<AppFactory>
{
    public QuerySelectionTests(ITestOutputHelper testOutputHelper, AppFactory factory) : base(testOutputHelper, factory)
    {
    }


    [Theory]
    [GenericData(nameof(GetTestData), Constants.DefaultODataRoutePrefix)]
    [CombineFrom([0], nameof(TestUtils.GetEntityType))]
    [CombineFrom([1], nameof(TestUtils.PickRandomProperties), AdditionalArguments = [2])]
    public async Task QuerySelectScalarProperties_Success<T>(TestData<T> testData, Type _, string[] properties)
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

        // Act
        var selectQuery = "?$select=" + string.Join(",", properties);

        var resp = await client.GetAsync($"{baseUrl}{selectQuery}");

        //Assert - returned json should have the same number of properties as the selected properties
        var jsonString = await resp.Content.ReadAsStringAsync();
        var propertyCount = TestUtils.GetJsonODataQueryPropertyCount(jsonString);
        var count = propertyCount.Distinct();
        count.Count().Should().Be(1);
        count.First().Should().Be(properties.Length);

        //Assert - value should be equal to the expected value
        var actual = jsonString.JsonConvert<ODataQueryResult<T>>(DefaultJsonSeriallizerOptions);
        actual.Should().NotBeNull();

        var pickByPropertiesExpected = expected.FilterProperties(properties);
        var pickByPropertiesActual = actual!.Value!.FilterProperties(properties);

        pickByPropertiesExpected.Should().BeEquivalentTo(pickByPropertiesActual);
    }
}