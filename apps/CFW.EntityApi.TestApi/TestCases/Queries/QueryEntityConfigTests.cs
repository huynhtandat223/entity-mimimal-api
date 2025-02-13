using CFW.EntityApi.Models;
using CFW.EntityApi.Models.Builders;
using CFW.EntityApi.TestApi;
using CFW.EntityApi.TestApi.Infrastructures.DbContexts;
using Microsoft.AspNetCore.OData.Query;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace CFW.ODataCore.Testings.TestCases;

public class QueryEntityConfigTests : BaseTests, IAssemblyFixture<AppFactory>
{
    public QueryEntityConfigTests(ITestOutputHelper testOutputHelper, AppFactory factory)
        : base(testOutputHelper, factory, types: [typeof(Category), typeof(Product)])
    {
    }


    [Theory]
    [GenericData(nameof(GetTestData), Constants.DefaultODataRoutePrefix)]
    public async Task QueryDisableCount_SuccessWithNoTotalCountResponse<T>(TestData<T> testData)
        where T : class
    {
        // Arrange
        var factory = SetupEntityApi(testData.RoutePrefix, testData.DataProvider)
            .WithWebHostBuilder(b =>
            {
                b.ConfigureTestServices(services =>
                {
                    services.RemoveAll<Action<EntityApiContextBuilder>>();
                    services.AddSingleton<Action<EntityApiContextBuilder>>(builder =>
                    {
                        builder.ConfigureEntity<T>()
                            .ConfigureAllowQueryOptions(~AllowedQueryOptions.Count);
                    });
                });
            });

        var client = factory.CreateClient();
        var baseUrl = testData.Url;

        var db = factory.Services.GetRequiredService<AppDbContext>();
        await SeedDataIfNotAnyRecords<T>(6, db);

        // Act
        var t = await client.GetAsync($"{baseUrl}?$count=true");
        var e = await t.Content.ReadAsStringAsync();

        var responseData = await client.GetFromJsonAsync<ODataQueryResult<T>>($"{baseUrl}?$count=true"
            , DefaultJsonSeriallizerOptions);

        // Assert
        responseData.Should().NotBeNull();
        responseData!.TotalCount.Should().BeNull();
    }

    //[Theory]
    //[InlineData(typeof(Category))]
    //[InlineData(typeof(Product))]
    //public async Task QueryDisableFilter_SuccessWithAllData(Type dbModelType)
    //{
    //    // Arrange
    //    var dataCount = 6;
    //    var (client, initialData) = SetupAllowQueryOptions(~AllowedQueryOptions.Filter, dbModelType, dataCount);
    //    var baseUrl = dbModelType.GetAllSupportableMethodBaseUrl();
    //    var complexProps = dbModelType.GetComplexTypeProperties();

    //    // Act
    //    var response = await client.GetAsync($"{baseUrl}?$filter eq '{Guid.NewGuid().ToString()}'&$count=true");

    //    // Assert
    //    response.Should().BeSuccessful();
    //    var actual = response.GetODataQueryResult(dbModelType);

    //    //Filter is disabled, so all data should be returned
    //    actual.TotalCount.Should().Be(dataCount);
    //    actual.Value.Should().BeEquivalentTo(initialData, o => o.Excluding(e => complexProps.Contains(e.Name)));
    //}

    //[Theory]
    //[InlineData(typeof(Category))]
    //[InlineData(typeof(Product))]
    //public async Task QueryDisableOrderBy_SuccessWithAllData(Type dbModelType)
    //{
    //    // Arrange

    //    var dataCount = 6;
    //    var (client, initialData) = SetupAllowQueryOptions(~AllowedQueryOptions.OrderBy, dbModelType, dataCount);
    //    var baseUrl = dbModelType.GetAllSupportableMethodBaseUrl();
    //    var complexProps = dbModelType.GetComplexTypeProperties();

    //    // Act
    //    var response = await client.GetAsync($"{baseUrl}?$orderby={Guid.NewGuid().ToString()}&$count=true");

    //    // Assert
    //    response.Should().BeSuccessful();
    //    var actual = response.GetODataQueryResult(dbModelType);

    //    //OrderBy is disabled, so all data should be returned
    //    actual.TotalCount.Should().Be(dataCount);
    //    actual.Value.Should()
    //        .BeEquivalentTo(initialData, o => o.Excluding(e => complexProps.Contains(e.Name)));
    //}

    //[Theory(Skip = "Select can't disable ????")]
    //[InlineData(typeof(Category))]
    //[InlineData(typeof(Product))]
    //public async Task QueryDisableSelect_SuccessWithAllData(Type dbModelType)
    //{
    //    // Arrange
    //    var dataCount = 6;
    //    var (client, initialData) = SetupAllowQueryOptions(~AllowedQueryOptions.Select, dbModelType, dataCount);
    //    var baseUrl = dbModelType.GetAllSupportableMethodBaseUrl();
    //    var complexProps = dbModelType.GetComplexTypeProperties();

    //    var randomProperties = dbModelType
    //        .GetProperties()
    //        .Where(x => !complexProps.Contains(x.Name))
    //        .Select(x => x.Name)
    //        .Random(2);
    //    var selectQuery = "?$select=" + string.Join(",", randomProperties);

    //    // Act
    //    var response = await client.GetAsync($"{baseUrl}{selectQuery}");

    //    // Assert
    //    response.Should().BeSuccessful();
    //    var actual = response.GetODataQueryResult(dbModelType);

    //    //Select is disabled, so all data should be returned
    //    actual.Value.Should()
    //        .BeEquivalentTo(initialData, o => o.Excluding(e => complexProps.Contains(e.Name)));
    //}

    //[Theory]
    //[InlineData(typeof(Category))]
    //[InlineData(typeof(Product))]
    //public async Task QueryDisableSkip_SuccessWithAllData(Type dbModelType)
    //{
    //    // Arrange
    //    var dataCount = 6;
    //    var (client, initialData) = SetupAllowQueryOptions(~AllowedQueryOptions.Skip, dbModelType, dataCount);
    //    var baseUrl = dbModelType.GetAllSupportableMethodBaseUrl();
    //    var complexProps = dbModelType.GetComplexTypeProperties();

    //    // Act
    //    var response = await client.GetAsync($"{baseUrl}?$skip=1&$count=true");

    //    // Assert
    //    response.Should().BeSuccessful();
    //    var actual = response.GetODataQueryResult(dbModelType);

    //    //Skip is disabled, so all data should be returned
    //    actual.TotalCount.Should().Be(dataCount);
    //    actual.Value.Should()
    //        .BeEquivalentTo(initialData, o => o.Excluding(e => complexProps.Contains(e.Name)));
    //}

    //[Theory]
    //[InlineData(typeof(Category))]
    //[InlineData(typeof(Product))]
    //public async Task QueryDisableTop_SuccessWithAllData(Type dbModelType)
    //{
    //    // Arrange
    //    var dataCount = 6;
    //    var (client, initialData) = SetupAllowQueryOptions(~AllowedQueryOptions.Top, dbModelType, dataCount);
    //    var baseUrl = dbModelType.GetAllSupportableMethodBaseUrl();
    //    var complexProps = dbModelType.GetComplexTypeProperties();

    //    // Act
    //    var response = await client.GetAsync($"{baseUrl}?$top=1&$count=true");

    //    // Assert
    //    response.Should().BeSuccessful();
    //    var actual = response.GetODataQueryResult(dbModelType);

    //    //Top is disabled, so all data should be returned
    //    actual.TotalCount.Should().Be(dataCount);
    //    actual.Value.Should()
    //        .BeEquivalentTo(initialData, o => o.Excluding(e => complexProps.Contains(e.Name)));
    //}

    //[Theory]
    //[InlineData(typeof(Category))]
    //[InlineData(typeof(Product))]
    //public async Task QueryDisableTopAndSkip_SuccessWithAllData(Type dbModelType)
    //{
    //    // Arrange
    //    var dataCount = 6;
    //    var (client, initialData) = SetupAllowQueryOptions(~AllowedQueryOptions.Top & ~AllowedQueryOptions.Skip, dbModelType, dataCount);
    //    var baseUrl = dbModelType.GetAllSupportableMethodBaseUrl();
    //    var complexProps = dbModelType.GetComplexTypeProperties();

    //    // Act

    //    var response = await client.GetAsync($"{baseUrl}?$top=1&$skip=1&$count=true");
    //    // Assert
    //    response.Should().BeSuccessful();
    //    var actual = response.GetODataQueryResult(dbModelType);

    //    //Top and Skip are disabled, so all data should be returned
    //    actual.TotalCount.Should().Be(dataCount);
    //    actual.Value.Should()
    //        .BeEquivalentTo(initialData, o => o.Excluding(e => complexProps.Contains(e.Name)));
    //}

    //[Theory]
    //[InlineData(typeof(Product))]
    //public async Task QueryDisableExpand_SuccessWithAllData(Type dbModelType)
    //{
    //    // Arrange
    //    var dataCount = 6;
    //    var expandProps = dbModelType.GetComplexTypeProperties();
    //    var (client, initialData) = SetupAllowQueryOptions(~AllowedQueryOptions.Expand, dbModelType, dataCount);
    //    var baseUrl = dbModelType.GetAllSupportableMethodBaseUrl();

    //    // Act
    //    var expandQuery = string.Join(",", expandProps);
    //    var response = await client.GetAsync($"{baseUrl}?$expand={expandQuery}");

    //    // Assert
    //    response.Should().BeSuccessful();
    //    var actual = response.GetODataQueryResult(dbModelType);

    //    //Assert - Returned expand data should be null
    //    var actualDataWithExpandProps = actual.Value
    //        .Select(expandProps);
    //    foreach (var actualDataWithExpandProp in actualDataWithExpandProps)
    //    {
    //        actualDataWithExpandProp.Values.Should().AllSatisfy(x => x.Should().BeNull());
    //    }

    //    //Rest of the data should be valid
    //    actual.Value.Should().BeEquivalentTo(initialData, o => o.Excluding(e => expandProps.Contains(e.Name)));
    //}
}
