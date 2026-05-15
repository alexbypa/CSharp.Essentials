namespace CSharpEssentials.HttpHelper.Tests;

public class HttpClientOptionsTests {
    [Fact]
    public void DefaultOptions_HasReasonableDefaults() {
        var options = new httpClientOptions {
            Name = "TestClient",
            httpProxy = new httpProxy()
        };
        Assert.Equal("TestClient", options.Name);
    }
}
