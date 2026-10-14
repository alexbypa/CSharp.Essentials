using Xunit.Abstractions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using System.Net;

namespace CSharpEssentials.LoggerHelper.Tests;

[Collection("Integration")]
public class IntegrationTests : IClassFixture<WebApplicationFactory<Program>> {
    private readonly ITestOutputHelper _output;

    private readonly WebApplicationFactory<Program> _factory;

    public IntegrationTests(WebApplicationFactory<Program> factory, ITestOutputHelper output) {
        _output = output;
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
        _output.WriteLine("[Scenario] WebApplicationFactory sul TestApp (rotta Console/Information, OTel disabilitato): GET /api");
        _output.WriteLine("[Atteso] HTTP 200 OK");

        var client = _factory.CreateClient();

        var response = await client.GetAsync("/api");

        _output.WriteLine($"[Restituito] StatusCode={(int)response.StatusCode} {response.StatusCode}");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task TestApp_RootEndpoint_ReturnsExpectedContent() {
        _output.WriteLine("[Scenario] WebApplicationFactory sul TestApp: GET /api e lettura del body come stringa");
        _output.WriteLine("[Atteso] Il body contiene \"LoggerHelper v5 is working!\"");

        var client = _factory.CreateClient();

        var content = await client.GetStringAsync("/api");

        _output.WriteLine($"[Restituito] Body=\"{content}\"");
        Assert.Contains("LoggerHelper v5 is working!", content);
    }

    [Fact]
    public async Task TestApp_MultipleRequests_AllSucceed() {
        _output.WriteLine("[Scenario] WebApplicationFactory sul TestApp: 10 GET /api consecutive con lo stesso HttpClient (il logging non deve degradare le richieste)");
        _output.WriteLine("[Atteso] Tutte e 10 le risposte sono HTTP 200 OK");

        var client = _factory.CreateClient();

        var statuses = new List<HttpStatusCode>();
        for (int i = 0; i < 10; i++) {
            var response = await client.GetAsync("/api");
            statuses.Add(response.StatusCode);
        }

        _output.WriteLine($"[Restituito] {statuses.Count} risposte: {string.Join(", ", statuses.Select(s => (int)s).Distinct())}");
        Assert.All(statuses, s => Assert.Equal(HttpStatusCode.OK, s));
    }
}
