using CFW.EntityApi.TestApi.Infrastructures.UnitTests;
using CFW.EntityApi.TestApi.Models;
using FluentAssertions;
using Xunit.Abstractions;

namespace CFW.EntityApi.TestApi.TestCases;


public class QueryTests : BaseTests, IAssemblyFixture<AppFactory>
{
    public QueryTests(ITestOutputHelper testOutputHelper, AppFactory factory) : base(testOutputHelper, factory)
    {
    }

    [Theory]
    [InlineData(typeof(Category))]
    public async Task Test(Type dbModelType)
    {
        // Arrange
        var client = _factory.CreateClient();
        var baseUrl = $"{Constants.DefaultODataRoutePrefix}/categories";
        //var complexProps = dbModelType.GetComplexTypeProperties();

        var initialData = await SeedData(dbModelType, 6);

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
