﻿using CFW.CoreTestings.DataGenerations;
using CFW.ODataCore.Testings.Models;
using System.Net;

namespace CFW.ODataCore.Testings.TestCases.EntitySetsDelete;

public class NoRelationshipDeleteTests : BaseTests, IClassFixture<AppFactory>
{
    public NoRelationshipDeleteTests(ITestOutputHelper testOutputHelper, AppFactory factory)
        : base(testOutputHelper, factory)
    {
    }

    [Theory]
    [InlineData(typeof(Category))]
    public async Task Delete_InvalidId_ShouldError(Type resourceType)
    {
        // Arrange
        var client = _factory.CreateClient();
        var baseUrl = resourceType.GetBaseUrl();
        // Act
        var resp = await client.DeleteAsync($"{baseUrl}/{DataGenerator.NewGuidString()}");
        resp.IsSuccessStatusCode.Should().BeFalse();
    }

    [Theory]
    [InlineData(typeof(Category))]
    public async Task Delete_ValidId_ShouldSuccess(Type resourceType)
    {
        // Arrange
        var client = _factory.CreateClient();
        var baseUrl = resourceType.GetBaseUrl();
        var idProp = nameof(IODataViewModel<object>.Id);

        var expectedEntity = DataGenerator.Create(resourceType);
        var resp = await client.PostAsJsonAsync(baseUrl, expectedEntity);
        resp.IsSuccessStatusCode.Should().BeTrue();
        var seededEntity = await resp.Content.ReadFromJsonAsync(resourceType);
        seededEntity.Should().NotBeNull();

        // Act
        var id = seededEntity!.GetPropertyValue(idProp);
        var deleteResp = await client.DeleteAsync($"{baseUrl}/{id}");

        // Assert
        deleteResp.IsSuccessStatusCode.Should().BeTrue();

        var getResp = await client.GetAsync($"{baseUrl}/{id}");
        getResp.IsSuccessStatusCode.Should().BeFalse();
        getResp.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}
