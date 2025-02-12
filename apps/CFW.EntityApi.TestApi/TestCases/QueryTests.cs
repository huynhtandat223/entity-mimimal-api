using CFW.EntityApi.TestApi.Infrastructures.DbContexts;
using CFW.EntityApi.TestApi.Infrastructures.UnitTests;
using CFW.EntityApi.TestApi.Models;
using Xunit.Abstractions;

namespace CFW.EntityApi.TestApi.TestCases;

public class QueryTests : BaseTests, IAssemblyFixture<AppFactory>
{
    public QueryTests(ITestOutputHelper testOutputHelper, AppFactory factory) : base(testOutputHelper, factory)
    {
    }

    [Theory]
    [InlineData(typeof(Category))]
    //[InlineData(typeof(Product))]
    public async Task Test(Type dbModelType)
    {
        // Arrange
        var routePrefix = Constants.DefaultODataRoutePrefix;
        var factory = SetupEntityApi(routePrefix);

        var baseUrl = $"{routePrefix}/categories";
        var complexProps = dbModelType.GetComplexTypeProperties();
        var client = factory.CreateClient();
        var db = factory.Services.CreateScope().ServiceProvider.GetRequiredService<AppDbContext>();
        var initialData = await SeedData(dbModelType, 6, db);

        // Act
        var response = await client.GetAsync(baseUrl);

        // Assert
        response.Should().BeSuccessful();
        var actual = response.GetODataQueryResult(dbModelType);

        actual.TotalCount.Should().BeNull();

        var expected = initialData.OfType<object>();
        actual.Value.Should()
            .BeEquivalentTo(expected, o => o.Excluding(e => complexProps.Contains(e.Name)));
    }
}
