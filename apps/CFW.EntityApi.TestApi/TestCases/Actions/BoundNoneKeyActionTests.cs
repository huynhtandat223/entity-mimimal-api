//using CFW.EntityApi.TestApi.Features.Categories;

//namespace CFW.EntityApi.TestApi.TestCases.Actions;

//public class BoundNoneKeyActionTests : BaseTests, IAssemblyFixture<AppFactory>
//{
//    public BoundNoneKeyActionTests(ITestOutputHelper testOutputHelper
//        , AppFactory factory) : base(testOutputHelper, factory)
//    {
//    }

//    [Theory]
//    [InlineData(typeof(CategoriesPingPongWithNonKeyNoResponseDataAttr.Handler)
//        , typeof(CategoriesPingPongWithNonKeyNoResponseDataAttr.RequestPing))]
//    public async Task Execute_BoundNoneKeyAction_DefaultPostMethod_EntityMultipleEndpoint_NoResponseData_Success(Type handlerType, Type requestType)
//    {
//        // Arrange
//        var (baseUrl, attr) = handlerType.GetNonKeyActionUrl();
//        var request = DataGenerator.Create(requestType);
//        var client = _factory.CreateClient();

//        // Act
//        var response = await client
//            .PostAsJsonAsync(baseUrl, request);

//        // Assert
//        response.Should().BeSuccessful();
//    }

//    [Theory]
//    [InlineData(typeof(CategoriesPingPongPatchMethodNoResponseData.Handler)
//        , typeof(CategoriesPingPongPatchMethodNoResponseData.RequestPing))]
//    public async Task Execute_BoundNoneKeyAction_PatchMethod_EntityMultipleEndpoint_NoResponseData_Success(Type handlerType
//        , Type requestType)
//    {
//        // Arrange
//        var (baseUrl, attr) = handlerType.GetNonKeyActionUrl();
//        var request = DataGenerator.Create(requestType);
//        var client = _factory.CreateClient();

//        // Act
//        var response = await client
//            .PatchAsJsonAsync(baseUrl, request);

//        // Assert
//        response.Should().BeSuccessful();
//    }

//    [Theory]
//    [InlineData(typeof(CategoriesPingPongWithNonKey.Handler)
//        , typeof(CategoriesPingPongWithNonKey.RequestPing)
//        , typeof(CategoriesPingPongWithNonKey.ResponsePong))]
//    public async Task Execute_BoundNoneKeyAction_DefaultPostMethod_EntityMultipleEndpoint_WithResponseData_Success(Type handlerType
//        , Type requestType, Type responseType)
//    {
//        // Arrange
//        var (baseUrl, attr) = handlerType.GetNonKeyActionUrl();
//        var request = DataGenerator.Create(requestType);
//        var client = _factory.CreateClient();

//        // Act
//        var response = await client
//            .PostAsJsonAsync(baseUrl, request);

//        // Assert
//        response.Should().BeSuccessful();
//        var actualResponse = response.GetResponseResult(responseType);
//        actualResponse.Should().BeEquivalentTo(request);
//    }

//    [Theory]
//    [InlineData(typeof(CategoriesPingPongPutMethodNonKey.Handler)
//        , typeof(CategoriesPingPongPutMethodNonKey.RequestPing)
//        , typeof(CategoriesPingPongPutMethodNonKey.ResponsePong))]
//    public async Task Execute_BoundNoneKeyAction_PutMethod_EntityMultipleEndpoint_WithResponseData_Success(Type handlerType
//        , Type requestType, Type responseType)
//    {
//        // Arrange
//        var (baseUrl, attr) = handlerType.GetNonKeyActionUrl();
//        var request = DataGenerator.Create(requestType);
//        var client = _factory.CreateClient();

//        // Act
//        var response = await client
//            .PutAsJsonAsync(baseUrl, request);

//        // Assert
//        response.Should().BeSuccessful();
//        var actualResponse = response.GetResponseResult(responseType);
//        actualResponse.Should().BeEquivalentTo(request);
//    }

//    [Theory]
//    [InlineData(typeof(CategoriesGetPingPong.Handler)
//        , typeof(CategoriesGetPingPong.RequestPing)
//        , typeof(CategoriesGetPingPong.ResponsePong))]
//    public async Task Execute_BoundNoneKeyAction_GetMethod_EntityMultipleEndpoint_WithResponseData_Success(Type handlerType
//        , Type requestType, Type responseType)
//    {
//        // Arrange
//        var (baseUrl, attr) = handlerType.GetNonKeyActionUrl();
//        var request = DataGenerator.Create(requestType);
//        var client = _factory.CreateClient();

//        // Act
//        var query = request.ParseToQueryString();
//        var response = await client
//            .GetAsync($"{baseUrl}?{query}");

//        // Assert
//        response.Should().BeSuccessful();
//        var actualResponse = response.GetResponseResult(responseType);
//        actualResponse.Should().BeEquivalentTo(request);
//    }
//}
