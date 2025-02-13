using CFW.Core.EfCoreExtensions;
using CFW.EntityApi.Models;
using CFW.EntityApi.TestApi.Infrastructures.DbContexts;
using Microsoft.EntityFrameworkCore;

namespace CFW.EntityApi.TestApi.TestCases.Queries;

public class QueryExpandTests : BaseTests, IAssemblyFixture<AppFactory>
{
    public QueryExpandTests(ITestOutputHelper testOutputHelper, AppFactory factory) : base(testOutputHelper, factory)
    {
    }

    [Theory]
    [GenericData(nameof(GetTestData), Constants.DefaultODataRoutePrefix)]
    public async Task QueryExpand_Success<T>(TestData<T> testData)
        where T : class
    {
        // Arrange
        var factory = SetupEntityApi(testData.RoutePrefix, testData.DataProvider);

        var baseUrl = testData.Url;
        var client = factory.CreateClient();
        var db = factory.GetScopedService<AppDbContext>();
        var expandableProperties = db.Set<T>().GetComplexAndNavigationPropertyNames();

        if (expandableProperties.Length == 0)
        {
            _testOutputHelper.WriteLine("No expandable properties found for the entity");
            return;
        }

        var expected = await SeedDataIfNotAnyRecords<T>(6, db);

        // Act
        var expandQuery = "?expand=" + string.Join(",", expandableProperties);
        var actual = await client.GetFromJsonAsync<ODataQueryResult<T>>($"{baseUrl}{expandQuery}"
            , DefaultJsonSeriallizerOptions);

        // Assert
        actual.Should().NotBeNull();

        IQueryable<T> dbData = factory.GetScopedService<AppDbContext>()
            .Set<T>();
        foreach (var expandableProperty in expandableProperties)
        {
            dbData = dbData.Include(expandableProperty);
        }
        var expandedData = dbData.ToList();
        dbData.Should().BeEquivalentTo(actual!.Value);
    }
}
