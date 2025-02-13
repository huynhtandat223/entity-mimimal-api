using CFW.EntityApi.Models;
using CFW.EntityApi.TestApi.Infrastructures.DbContexts;

namespace CFW.EntityApi.TestApi.TestCases.Queries;

public class QueryTopSkipTests : BaseTests, IAssemblyFixture<AppFactory>
{
    public QueryTopSkipTests(ITestOutputHelper testOutputHelper, AppFactory factory) : base(testOutputHelper, factory)
    {
    }

    [Theory]
    [GenericData(nameof(GetTestData), Constants.DefaultODataRoutePrefix)]
    [Combine(2, 2)]
    public async Task QuerySimpleTopSkip_Success<T>(TestData<T> testData, int top, int skip)
        where T : class
    {
        // Arrange
        var factory = SetupEntityApi(testData.RoutePrefix, testData.DataProvider);

        var baseUrl = $"{testData.Url}?$skip={skip}&$top={top}&$orderby={testData.IdPropertyName}&$count=true";
        var client = factory.CreateClient();
        var db = factory.GetScopedService<AppDbContext>();

        var expected = await SeedDataIfNotAnyRecords<T>(11, db);

        // Act
        var actual = await client.GetFromJsonAsync<ODataQueryResult<T>>(baseUrl, DefaultJsonSeriallizerOptions);

        // Assert
        actual.Should().NotBeNull();

        var dbData = factory.GetScopedService<AppDbContext>().Set<T>().ToList();
        var orderedData = dbData.OrderByProperty(testData.IdPropertyName).ToList();

        //Total count should be equal to the actual count
        var dbTotalCount = dbData.Count;
        actual!.TotalCount.Should().Be(dbTotalCount);

        //Actual value count should be equal to the top
        actual.Value.Should().HaveCount(top);

        //Actual value should be equal to the expected value
        var orderedInitialData = dbData.OrderByProperty(testData.IdPropertyName);
        var expectedValues = orderedInitialData.Skip(skip).Take(top);

        actual.Value.Should().BeEquivalentTo(expectedValues, o => o
            .WithStrictOrdering());
    }
}

