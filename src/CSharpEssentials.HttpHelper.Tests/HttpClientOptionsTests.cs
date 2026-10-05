using Xunit.Abstractions;
using System.Net;

namespace CSharpEssentials.HttpHelper.Tests;

public class HttpClientOptionsTests {
    private readonly ITestOutputHelper _output;

    public HttpClientOptionsTests(ITestOutputHelper output) {
        _output = output;
    }

    [Fact]
    public void NewOptions_OnlyNameSet_OptionalMembersDefault() {
        _output.WriteLine("[Scenario] Creo httpClientOptions impostando solo Name = \"TestClient\"");
        _output.WriteLine("[Atteso] Name valorizzato; Mock, Certificate, RateLimitOptions e httpProxy sono null; UseCompression == false");

        var options = new httpClientOptions { Name = "TestClient" };

        _output.WriteLine($"[Restituito] Name={options.Name}, Mock={options.Mock?.ToString() ?? "null"}, Certificate={(options.Certificate is null ? "null" : "valorizzato")}, RateLimitOptions={(options.RateLimitOptions is null ? "null" : "valorizzato")}, httpProxy={(options.httpProxy is null ? "null" : "valorizzato")}, UseCompression={options.UseCompression}");
        Assert.Equal("TestClient", options.Name);
        Assert.Null(options.Mock);
        Assert.Null(options.Certificate);
        Assert.Null(options.RateLimitOptions);
        Assert.Null(options.httpProxy);
        Assert.False(options.UseCompression);
    }

    [Fact]
    public void NewRateLimitOptions_Defaults_AreDisabledAndZero() {
        _output.WriteLine("[Scenario] Creo httpClientRateLimitOptions senza valorizzare nulla");
        _output.WriteLine("[Atteso] Rate limiting disattivo di default: IsEnabled=false, AutoReplenishment=false, PermitLimit=QueueLimit=SegmentsPerWindow=0, Window=TimeSpan.Zero");

        var options = new httpClientRateLimitOptions();

        _output.WriteLine($"[Restituito] IsEnabled={options.IsEnabled}, AutoReplenishment={options.AutoReplenishment}, PermitLimit={options.PermitLimit}, QueueLimit={options.QueueLimit}, SegmentsPerWindow={options.SegmentsPerWindow}, Window={options.Window}");
        Assert.False(options.IsEnabled);
        Assert.False(options.AutoReplenishment);
        Assert.Equal(0, options.PermitLimit);
        Assert.Equal(0, options.QueueLimit);
        Assert.Equal(0, options.SegmentsPerWindow);
        Assert.Equal(TimeSpan.Zero, options.Window);
    }

    [Fact]
    public void NewHttpProxy_Defaults_UseProxyFalse() {
        _output.WriteLine("[Scenario] Creo httpProxy senza valorizzare nulla");
        _output.WriteLine("[Atteso] UseProxy == false: il proxy è disattivato di default");

        var proxy = new httpProxy();

        _output.WriteLine($"[Restituito] UseProxy={proxy.UseProxy}");
        Assert.False(proxy.UseProxy);
    }
}
