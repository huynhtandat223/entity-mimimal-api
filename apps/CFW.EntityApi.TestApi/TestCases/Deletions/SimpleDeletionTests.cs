using CFW.EntityApi.TestApi.Infrastructures.DbContexts;
using Microsoft.EntityFrameworkCore;

namespace CFW.EntityApi.TestApi.TestCases.Deletions;

public class SimpleDeletionTests : BaseTests, IAssemblyFixture<AppFactory>
{
    public SimpleDeletionTests(ITestOutputHelper testOutputHelper, AppFactory factory) : base(testOutputHelper, factory)
    {
    }

    [Theory]
    [GenericData(nameof(GetTestData), Constants.DefaultODataRoutePrefix)]
    public async Task DeleteEntity_NoConfiguration_Success<T>(TestData<T> testData)
        where T : class
    {
        // Arrange
        var factory = SetupEntityApi(testData.RoutePrefix, testData.DataProvider);

        var baseUrl = testData.Url;
        var client = factory.CreateClient();
        var db = factory.GetScopedService<AppDbContext>();

        await SeedDataIfNotAnyRecords<T>(6, db);
        var dbEntity = await db.Set<T>().FirstAsync();
        var id = dbEntity.GetPropertyValue(DefaultIdProp);
        var url = $"{baseUrl}/{id}";

        // Act
        var resp = await client.DeleteAsync(url);

        // Assert
        resp.Should().BeSuccessful();
        db = factory.GetScopedService<AppDbContext>();
        var actual = await db.Set<T>().FindAsync(id);

        actual.Should().BeNull();
    }
}
