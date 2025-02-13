using CFW.EntityApi.TestApi.Features.Categories;

namespace CFW.EntityApi.TestApi.TestCases.Actions;

public class EntityActionTests : BaseTests, IAssemblyFixture<AppFactory>
{
    public EntityActionTests(ITestOutputHelper testOutputHelper
        , AppFactory factory) : base(testOutputHelper, factory)
    {
    }

    public static IEnumerable<object[]> RequestResponseActionData()
    {
        yield return new object[] {
            new TestAction<CategoriesGetPingPong.RequestPing, CategoriesGetPingPong.ResponsePong> {
                ActionRoute =  "categories/getPingPong"
            }
        };
    }

    [Theory]
    [MemberData(nameof(RequestResponseActionData))]
    public async Task RequestResponse_Success<TRequest, TResponse>(
        TestAction<TRequest, TResponse> action)
    {
        // Arrange
        var factory = SetupEntityApi(Constants.DefaultODataRoutePrefix, DataProvider.Sqlite);

        var request = DataGenerator.Create<TRequest>();
        var query = request!.ParseToQueryString();

        var client = factory.CreateClient();
        var url = $"{Constants.DefaultODataRoutePrefix}/{action.ActionRoute}?{query}";

        // Act
        var response = await client
            .GetFromJsonAsync<TResponse>(url);

        // Assert
        response.Should().NotBeNull();
        requestObjects.Should().Contain(response!);
    }
}
