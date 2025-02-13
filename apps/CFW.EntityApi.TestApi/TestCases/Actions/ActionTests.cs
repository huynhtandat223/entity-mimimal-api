using CFW.EntityApi.TestApi.Features.Unspecified;

namespace CFW.EntityApi.TestApi.TestCases.Actions;

public class ActionTests : BaseTests, IAssemblyFixture<AppFactory>
{
    public ActionTests(ITestOutputHelper testOutputHelper
        , AppFactory factory) : base(testOutputHelper, factory)
    {
    }

    public static IEnumerable<object[]> RequestResponseActionData()
    {
        yield return new object[] {
            new TestAction<PostPingPong.RequestPing, PostPingPong.ResponsePong> {
                ActionRoute =  "unspecified/pingPong"
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

        var client = factory.CreateClient();
        var url = $"{Constants.DefaultODataRoutePrefix}/{action.ActionRoute}";

        // Act
        var response = await client
            .PostAsJsonAsync(url, request, DefaultJsonSeriallizerOptions);
        var responseContent = await response.Content.ReadAsStringAsync();
        var responseData = responseContent.JsonConvert<TResponse>(DefaultJsonSeriallizerOptions);

        // Assert
        responseData.Should().NotBeNull();
        requestObjects.Should().Contain(responseData!);
    }
}

