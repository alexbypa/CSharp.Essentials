using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using System.Net;

namespace CSharpEssentials.LoggerHelper.Tests;

[Collection("Integration")]
public class IntegrationTests : IClassFixture<WebApplicationFactory<Program>> {
    private readonly WebApplicationFactory<Program> _factory;

    public IntegrationTests(WebApplicationFactory<Program> factory) {
        _factory = factory.WithWebHostBuilder(builder => {
            builder.UseEnvironment("Testing");
            builder.ConfigureAppConfiguration((_, config) => {
                config.AddInMemoryCollection(new Dictionary<string, string?> {
                    ["LoggerHelper:ApplicationName"] = "IntegrationTest",
                    ["LoggerHelper:Routes:0:Sink"] = "Console",
                    ["LoggerHelper:Routes:0:Levels:0"] = "Information",
                    ["LoggerHelper:General:EnableOpenTelemetry"] = "false"
                });
            });
        });
    }

    [Fact]
    public async Task TestApp_RootEndpoint_Returns200() {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/api");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task TestApp_RootEndpoint_ReturnsExpectedContent() {
        var client = _factory.CreateClient();

        var content = await client.GetStringAsync("/api");

        Assert.Contains("LoggerHelper v5 is working!", content);
    }

    [Fact]
    public async Task TestApp_MultipleRequests_AllSucceed() {
        var client = _factory.CreateClient();

        for (int i = 0; i < 10; i++) {
            var response = await client.GetAsync("/api");
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }
    }
}
