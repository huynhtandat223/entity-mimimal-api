using CFW.EntityApi.TestApi.Infrastructures.DbContexts;
using Microsoft.EntityFrameworkCore;

namespace CFW.EntityApi.TestApi.TestCases.Creations;

public class SimpleCreationTests : BaseTests, IAssemblyFixture<AppFactory>
{
    public SimpleCreationTests(ITestOutputHelper testOutputHelper, AppFactory factory) : base(testOutputHelper, factory)
    {
    }

    [Theory]
    [GenericData(nameof(GetTestData), Constants.DefaultODataRoutePrefix)]
    public async Task CreateEntity_NoConfiguration_Success<T>(TestData<T> testData)
        where T : class
    {
        // Arrange
        var factory = SetupEntityApi(testData.RoutePrefix, testData.DataProvider);

        var baseUrl = testData.Url;
        var client = factory.CreateClient();
        var entity = DataGenerator.Create<T>();

        var db = factory.GetScopedService<AppDbContext>();
        var complexProps = db.Set<T>().EntityType.GetComplexProperties().ToList();
        foreach (var complexProp in complexProps)
        {
            var complexPropValue = DataGenerator.Create(complexProp.ClrType);
            entity.SetPropertyValue(complexProp.Name, complexPropValue);
        }

        var navigationProps = db.Set<T>().EntityType.GetNavigations().ToList();
        foreach (var navigationProp in navigationProps)
        {
            var navigationPropValue = DataGenerator.Create(navigationProp.ClrType);
            entity.SetPropertyValue(navigationProp.Name, navigationPropValue);
        }

        // Act
        var response = await client.PostAsJsonAsync(baseUrl, entity);

        // Assert
        response.Should().BeSuccessful();

        db = factory.GetScopedService<AppDbContext>();
        IQueryable<T> actual = db.Set<T>();
        foreach (var complexProp in complexProps)
        {
            actual = actual.Include(complexProp.Name);
        }

        foreach (var navigationProp in navigationProps)
        {
            actual = actual.Include(navigationProp.Name);
        }

        var id = entity.GetPropertyValue(DefaultIdProp);
        var equalExpr = ExpressionUtils.BuilderEqualExpression<T>(id!, DefaultIdProp);

        var actualData = actual.Where(equalExpr).FirstOrDefault();

        actualData.Should().BeEquivalentTo(entity, o => o.WithoutStrictOrdering());
    }
}
