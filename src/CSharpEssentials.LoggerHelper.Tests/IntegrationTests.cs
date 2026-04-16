using Microsoft.AspNetCore.Mvc.Testing;
using System.Net;

namespace CSharpEssentials.LoggerHelper.Tests;

public class IntegrationTests : IClassFixture<WebApplicationFactory<Program>> {
    private readonly WebApplicationFactory<Program> _factory;

    public IntegrationTests(WebApplicationFactory<Program> factory) {
        _factory = factory;
    }

    [Fact]
    public async Task TestApp_RootEndpoint_Returns200() {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task TestApp_RootEndpoint_ReturnsExpectedContent() {
        var client = _factory.CreateClient();

        var content = await client.GetStringAsync("/");

        Assert.Equal("LoggerHelper v5 is working!", content);
    }

    [Fact]
    public async Task TestApp_MultipleRequests_AllSucceed() {
        var client = _factory.CreateClient();

        for (int i = 0; i < 10; i++) {
            var response = await client.GetAsync("/");
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }
    }
}
