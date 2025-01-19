
using CFW.ODataCore.Models;
using CFW.ODataCore.Testings.Features.Payments;
using CFW.ODataCore.Testings.Models;
using System.Linq.Expressions;

namespace CFW.ODataCore.Testings.TestCases;

public class EntityConfigurationTests : BaseTests, IAssemblyFixture<AppFactory>
{
    public EntityConfigurationTests(ITestOutputHelper testOutputHelper, AppFactory factory)
        : base(testOutputHelper, factory, types: [typeof(Payment), typeof(PaymentEndpointConfiguration)])
    {
    }

    [Theory]
    [InlineData(typeof(Payment), typeof(PaymentEndpointConfiguration), nameof(Payment.Orders))]
    public async Task CreateEntity_UseConfiguration_Success(Type dbModelType
        , Type configurationType, string collectionProperty)
    {
        // Arrange
        var client = _factory.CreateClient();
        var baseUrl = configurationType.GetAllSupportableMethodBaseUrl();
        var entity = DataGenerator.Create(dbModelType);

        //find test metadata
        var configInstance = Activator.CreateInstance(configurationType);
        var model = configInstance!.GetPropertyValue(nameof(EntityEndpoint<object>.Model)) as LambdaExpression;

        if (model!.Body is not MemberInitExpression memberInitExpression)
        {
            throw new NotImplementedException();
        }
        var allowPropertyNames = memberInitExpression.Bindings!.Select(x => x.Member.Name).ToArray();
        var excludeProperties = dbModelType.GetProperties()
            .Where(x => !allowPropertyNames.Contains(x.Name))
            .Select(x => x.Name);

        //set empty for excluded properties
        foreach (var excludeProperty in excludeProperties)
        {
            entity.SetPropertyValue(excludeProperty, null);
        }

        var collectionPropertyInfo = dbModelType.GetProperty(collectionProperty);
        var elementType = collectionPropertyInfo!.PropertyType.GetGenericArguments()[0];
        entity.SetPropertyValue(collectionProperty, DataGenerator.CreateList(elementType, 3));

        // Act
        var response = await client.PostAsJsonAsync(baseUrl, entity);

        // Assert
        response.Should().BeSuccessful();
        var db = GetDbContext();
        var id = entity.GetPropertyValue(DefaultIdProp);
        var actual = await db.LoadAsync(dbModelType, [id!]);

        actual.Should().BeEquivalentTo(entity, o => o
            .Excluding(e => excludeProperties.Contains(e.Name))
            .WithoutStrictOrdering());
    }

    [Fact]
    public async Task CreateEntity_UseConfiguration_ExistingNavigation_CreateSuccess()
    {
        var client = _factory.CreateClient();
        var baseUrl = typeof(PaymentEndpointConfiguration).GetAllSupportableMethodBaseUrl();
        var entity = DataGenerator.Create<Payment>();
    }
}
