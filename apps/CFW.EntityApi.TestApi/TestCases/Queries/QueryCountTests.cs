using CFW.Core.EfCoreExtensions;
using CFW.EntityApi.Models;
using CFW.EntityApi.TestApi.Infrastructures.DbContexts;

namespace CFW.EntityApi.TestApi.TestCases.Queries;

public class QueryCountTests : BaseTests, IAssemblyFixture<AppFactory>
{
    public QueryCountTests(ITestOutputHelper testOutputHelper, AppFactory factory) : base(testOutputHelper, factory)
    {
    }

    [Theory]
    [GenericData(nameof(GetTestData), Constants.DefaultODataRoutePrefix)]
    [CombineRandomValue(["?$count=true", "?count=true"])]
    [CombineRandomValue([null!, 5])]
    public async Task QueryCount_Success<T>(TestData<T> testData, string query
        , int? maxTop)
        where T : class
    {
        // Arrange
        var factory = SetupEntityApi(testData.RoutePrefix
            , testData.DataProvider
            , o =>
            {
                o.EnableNoDollarQueryOptions = !query.Contains("$");
                if (maxTop is not null) o.SetMaxTop(maxTop);
            });

        var baseUrl = $"{testData.Url}{query}";
        var client = factory.CreateClient();
        var db = factory.GetScopedService<AppDbContext>();

        var expected = await SeedDataIfNotAnyRecords<T>(11, db);

        // Act
        var actual = await client.GetFromJsonAsync<ODataQueryResult<T>>(baseUrl, DefaultJsonSeriallizerOptions);

        // Assert
        actual.Should().NotBeNull();
        actual!.TotalCount.Should().NotBeNull();

        //return maximum top if it is set
        if (maxTop is not null)
            actual.Value.Count().Should().Be(maxTop);

        //return default max top if it is not set
        if (maxTop is null)
            actual.Value.Count().Should().Be(DefaultPageSize);

        //Total count should be equal to the actual count
        var actualCount = factory.GetScopedService<AppDbContext>().Set<T>().Count();
        actual.TotalCount.Should().Be(actualCount);

        var scalarProps = db.Set<T>().GetScalarProperties();

        var scalarExpected = expected.FilterProperties(scalarProps);
        var scalarActual = actual.Value!.FilterProperties(scalarProps);

        scalarActual.Should()
            .BeEquivalentTo(scalarExpected);
    }
}
