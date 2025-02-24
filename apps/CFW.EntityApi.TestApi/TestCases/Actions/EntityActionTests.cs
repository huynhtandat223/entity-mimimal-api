using CFW.EntityApi.TestApi.Features.Categories;
using System.Net.Http.Headers;

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
                ActionRoute =  "categories/getPingPong",
                Method = EntityApi.Models.ApiMethod.Get
            }
        };
        yield return new object[] {
            new TestAction<CategoriesGetPingPong.RequestPing, CategoriesGetPingPong.ResponsePong> {
                ActionRoute =  "categories/postPingPong",
                Method = EntityApi.Models.ApiMethod.Post
            }
        };
        yield return new object[] {
            new TestAction<CategoriesGetPingPong.RequestPing, CategoriesGetPingPong.ResponsePong> {
                ActionRoute =  "categories/pingPong",
                Method = EntityApi.Models.ApiMethod.Post //default method.
            }
        };
        yield return new object[] {
            new TestAction<CategoriesGetPingPong.RequestPing, CategoriesGetPingPong.ResponsePong> {
                ActionRoute =  "categories/putPingPong",
                Method = EntityApi.Models.ApiMethod.Put
            }
        };
        yield return new object[] {
            new TestAction<CategoriesGetPingPong.RequestPing, CategoriesGetPingPong.ResponsePong> {
                ActionRoute =  "categories/patchPingPong",
                Method = EntityApi.Models.ApiMethod.Patch
            }
        };
        yield return new object[] {
            new TestAction<CategoriesGetPingPong.RequestPing, CategoriesGetPingPong.ResponsePong> {
                ActionRoute =  "categories/deletePingPong",
                Method = EntityApi.Models.ApiMethod.Delete
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

        // Act
        var httpMethod = action.Method switch
        {
            EntityApi.Models.ApiMethod.Get => HttpMethod.Get,
            EntityApi.Models.ApiMethod.Post => HttpMethod.Post,
            EntityApi.Models.ApiMethod.Put => HttpMethod.Put,
            EntityApi.Models.ApiMethod.Patch => HttpMethod.Patch,
            EntityApi.Models.ApiMethod.Delete => HttpMethod.Delete,
            _ => throw new NotImplementedException()
        };

        var url = $"{Constants.DefaultODataRoutePrefix}/{action.ActionRoute}";
        var requestMgs = new HttpRequestMessage(httpMethod, url);

        if (httpMethod != HttpMethod.Get)
        {
            requestMgs.Content = new StringContent(request!.ToJsonString());
            requestMgs.Content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
        }
        else
        {
            var query = request!.ParseToQueryString();
            requestMgs = new HttpRequestMessage(httpMethod, $"{url}?{query}");
        }

        var response = await client.SendAsync(requestMgs);

        var responseContent = await response.Content.ReadAsStringAsync();
        var responseData = responseContent.JsonConvert<TResponse>(DefaultJsonSeriallizerOptions);

        // Assert
        responseData.Should().NotBeNull();
        requestObjects.Should().Contain(responseData!);
        request.Should().BeEquivalentTo(responseData);
    }

    [Fact]
    public async Task HasRouteSameAsRequestProperty_RequestResponse_Success()
    {
        // Arrange
        var factory = SetupEntityApi(Constants.DefaultODataRoutePrefix, DataProvider.Sqlite);

        var request = DataGenerator.Create<CategoriesGetRouteIdPingPong.RequestPing>();
        var id = request.Id;

        var query = new { request.Name }!.ParseToQueryString();

        var client = factory.CreateClient();
        var url = $"{Constants.DefaultODataRoutePrefix}/categories/{id}/getPingPong?{query}";

        // Act
        var response = await client
            .GetFromJsonAsync<CategoriesGetRouteIdPingPong.ResponsePong>(url);

        // Assert
        response.Should().NotBeNull();
        requestObjects.Should().Contain(response!);
        request.Should().BeEquivalentTo(response);
    }

}
