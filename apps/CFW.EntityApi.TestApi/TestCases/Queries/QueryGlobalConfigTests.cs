using CFW.Core.EfCoreExtensions;
using CFW.EntityApi.Models;
using CFW.EntityApi.TestApi.Infrastructures.DbContexts;
using Microsoft.AspNetCore.OData.Query;

namespace CFW.EntityApi.TestApi.TestCases.Queries;

public class QueryGlobalConfigTests : BaseTests, IAssemblyFixture<AppFactory>
{
    public QueryGlobalConfigTests(ITestOutputHelper testOutputHelper, AppFactory factory)
        : base(testOutputHelper, factory, types: [typeof(Category), typeof(Product)])
    {
    }

    [Theory]
    [GenericData(nameof(GetTestData), Constants.DefaultODataRoutePrefix)]
    public async Task QueryDisableCount_SuccessWithNoTotalCountResponse<T>(TestData<T> testData)
        where T : class
    {
        // Arrange
        var factory = SetupEntityApi(testData.RoutePrefix, testData.DataProvider
            , containerApiBuilderSetup: b =>
            {
                b.ConfigureAllowQueryOptions(~AllowedQueryOptions.Count);
            });
        var client = factory.CreateClient();
        var baseUrl = testData.Url;

        var db = factory.Services.GetRequiredService<AppDbContext>();
        await SeedDataIfNotAnyRecords<T>(6, db);

        // Act
        var responseData = await client.GetFromJsonAsync<ODataQueryResult<T>>($"{baseUrl}?$count=true"
            , DefaultJsonSeriallizerOptions);

        // Assert
        responseData.Should().NotBeNull();
        responseData!.TotalCount.Should().BeNull();
    }

    [Theory]
    [GenericData(nameof(GetTestData), Constants.DefaultODataRoutePrefix)]
    [CombineFrom([0], nameof(TestUtils.GetEntityType))]
    [CombineFrom([1], nameof(TestUtils.PickRandomProperties), AdditionalArguments = [2])]
    public async Task QueryDisableFilter_SuccessWithAllData<T>(TestData<T> testData, Type _, string[] properties)
        where T : class
    {
        // Arrange
        var factory = SetupEntityApi(testData.RoutePrefix, testData.DataProvider
            , containerApiBuilderSetup: b => b.ConfigureAllowQueryOptions(~AllowedQueryOptions.Filter));

        var baseUrl = testData.Url;
        var client = factory.CreateClient();
        var db = factory.GetScopedService<AppDbContext>();
        var scalarProperties = db.Set<T>().GetScalarProperties();
        properties = properties.Intersect(scalarProperties).ToArray();

        if (properties.Length == 0)
        {
            _testOutputHelper.WriteLine("No scalar properties found for the entity");
            return;
        }

        var setupData = await SeedDataIfNotAnyRecords<T>(6, db);
        var randomPropertyName = properties.Random();
        var randomValue = setupData
            .Random()
            .GetPropertyValue(randomPropertyName);

        var filterValue = randomValue!.FormatOdataFilter();

        // Act
        var responseData = await client.GetFromJsonAsync<ODataQueryResult<T>>(
            $"{baseUrl}?$filter eq '{Guid.NewGuid().ToString()}'&$count=true"
            , DefaultJsonSeriallizerOptions);

        // Assert
        responseData.Should().NotBeNull();

        //Filter is disabled, so all data should be returned
        var dataCount = factory.GetScopedService<AppDbContext>().Set<T>().Count();
        responseData!.TotalCount.Should().Be(dataCount);
    }

    [Theory(Skip = "Can't support ???")]
    [GenericData(nameof(GetTestData), Constants.DefaultODataRoutePrefix)]
    [CombineFrom([0], nameof(TestUtils.GetEntityType))]
    [CombineFrom([1], nameof(TestUtils.PickRandomProperties), AdditionalArguments = [2])]
    [CombineRandomValue(typeof(bool), ValueCount = 2)]
    public async Task QueryDisableOrderBy_SuccessWithAllData<T>(TestData<T> testData, Type _
        , string[] properties, bool isAsc)
        where T : class
    {
        // Arrange
        var factory = SetupEntityApi(testData.RoutePrefix, testData.DataProvider
                    , containerApiBuilderSetup: b => b.ConfigureAllowQueryOptions(~AllowedQueryOptions.OrderBy));

        var baseUrl = testData.Url;
        var client = factory.CreateClient();
        var db = factory.GetScopedService<AppDbContext>();
        var scalaProperties = db.Set<T>().GetScalarProperties();
        properties = properties.Intersect(scalaProperties).ToArray();

        if (properties.Length == 0)
        {
            _testOutputHelper.WriteLine("No scalar properties found for the entity");
            return;
        }

        var expected = await SeedDataIfNotAnyRecords<T>(6, db);
        var randomProperty = properties.Random();

        // Act
        var orderByQuery = $"?$orderby={randomProperty}{(!isAsc ? " desc" : string.Empty)}";

        var actual = await client.GetFromJsonAsync<ODataQueryResult<T>>($"{baseUrl}{orderByQuery}"
            , DefaultJsonSeriallizerOptions);

        //Assert
        actual.Should().NotBeNull();

        //OrderBy is disabled, so order should be the same as the db data
        var dbData = factory.GetScopedService<AppDbContext>().Set<T>().ToList();
        actual!.Value.Should().BeEquivalentTo(dbData, o => o.WithStrictOrdering());
    }

    [Theory(Skip = "Can't support????")]
    [GenericData(nameof(GetTestData), Constants.DefaultODataRoutePrefix)]
    [CombineFrom([0], nameof(TestUtils.GetEntityType))]
    [CombineFrom([1], nameof(TestUtils.PickRandomProperties), AdditionalArguments = [2])]
    public async Task QueryDisableSelect_SuccessWithAllData<T>(TestData<T> testData, Type _, string[] properties)
        where T : class
    {
        // Arrange
        var factory = SetupEntityApi(testData.RoutePrefix, testData.DataProvider
            , containerApiBuilderSetup: o => o.ConfigureAllowQueryOptions(~AllowedQueryOptions.Select));

        var baseUrl = testData.Url;
        var client = factory.CreateClient();
        var db = factory.GetScopedService<AppDbContext>();
        var scalaProperties = db.Set<T>().GetScalarProperties();
        properties = properties.Intersect(scalaProperties).ToArray();

        if (properties.Length == 0)
        {
            _testOutputHelper.WriteLine("No scalar properties found for the entity");
            return;
        }

        var expected = await SeedDataIfNotAnyRecords<T>(6, db);

        // Act
        var selectQuery = "?$select=" + string.Join(",", properties);

        var responseData = await client.GetFromJsonAsync<ODataQueryResult<T>>($"{baseUrl}{selectQuery}");

        // Assert
        responseData.Should().NotBeNull();

        //Select is disabled, so all data should be returned
        var dbData = factory.GetScopedService<AppDbContext>().Set<T>().ToList();
        responseData!.Value.Should()
            .BeEquivalentTo(dbData);
    }


    [Theory]
    [GenericData(nameof(GetTestData), Constants.DefaultODataRoutePrefix)]
    public async Task QueryDisableExpand_SuccessWithAllData<T>(TestData<T> testData)
        where T : class
    {
        // Arrange
        var factory = SetupEntityApi(testData.RoutePrefix, testData.DataProvider
            , containerApiBuilderSetup: o => o.ConfigureAllowQueryOptions(~AllowedQueryOptions.Expand));

        var baseUrl = testData.Url;
        var client = factory.CreateClient();
        var db = factory.GetScopedService<AppDbContext>();
        var expandableProperties = db.Set<T>().GetComplexAndNavigationPropertyNames();

        if (expandableProperties.Length == 0)
        {
            _testOutputHelper.WriteLine("No expandable properties found for the entity");
            return;
        }

        var expected = await SeedDataIfNotAnyRecords<T>(6, db);

        // Act
        var expandQuery = "?expand=" + string.Join(",", expandableProperties);
        var actual = await client.GetFromJsonAsync<ODataQueryResult<T>>($"{baseUrl}{expandQuery}"
            , DefaultJsonSeriallizerOptions);

        // Assert
        actual.Should().NotBeNull();

        //expand is disabled, so only the primitive properties should be returned
        var dbData = factory.GetScopedService<AppDbContext>()
            .Set<T>()
            .ToList();

        //Disable Expand, so only the primitive properties should be returned
        actual!.Value.Should().BeEquivalentTo(dbData);
    }

    [Theory(Skip = "Global config can't set skip")]
    [InlineData(typeof(Category))]
    [InlineData(typeof(Product))]
    public Task QueryDisableSkip_SuccessWithAllData(Type dbModelType)
    {
        throw new NotImplementedException();
        //// Arrange
        //var dataCount = 6;
        //var (client, initialData) = SetupAllowQueryOptions(o => o.EnableSkipToken = false, dbModelType, dataCount); //Don't have EnableSkip property
        //var baseUrl = dbModelType.GetAllSupportableMethodBaseUrl();
        //var complexProps = dbModelType.GetComplexTypeProperties();

        //// Act
        //var response = await client.GetAsync($"{baseUrl}?$skip=1&$count=true");

        //// Assert
        //response.Should().BeSuccessful();
        //var actual = response.GetODataQueryResult(dbModelType);

        ////Skip is disabled, so all data should be returned
        //actual.TotalCount.Should().Be(dataCount);
        //actual.Value.Should()
        //    .BeEquivalentTo(initialData, o => o.Excluding(e => complexProps.Contains(e.Name)));
    }

    [Theory(Skip = "Global config can't set top")]
    [InlineData(typeof(Category))]
    [InlineData(typeof(Product))]
    public Task QueryDisableTop_SuccessWithAllData(Type dbModelType)
    {
        throw new NotImplementedException();

        //// Arrange
        //var dataCount = 6;
        //var (client, initialData) = SetupAllowQueryOptions(o => o.MaxTop = null, dbModelType, dataCount); //Don't have EnableTop property
        //var baseUrl = dbModelType.GetAllSupportableMethodBaseUrl();
        //var complexProps = dbModelType.GetComplexTypeProperties();

        //// Act
        //var response = await client.GetAsync($"{baseUrl}?$top=1&$count=true");

        //// Assert
        //response.Should().BeSuccessful();
        //var actual = response.GetODataQueryResult(dbModelType);

        ////Top is disabled, so all data should be returned
        //actual.TotalCount.Should().Be(dataCount);
        //actual.Value.Should()
        //    .BeEquivalentTo(initialData, o => o.Excluding(e => complexProps.Contains(e.Name)));
    }
}
