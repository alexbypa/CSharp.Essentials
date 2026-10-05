using Xunit.Abstractions;
using System.Net;

namespace CSharpEssentials.HttpHelper.Tests;

public class ProxyConfiguratorTests {
    private readonly ITestOutputHelper _output;

    public ProxyConfiguratorTests(ITestOutputHelper output) {
        _output = output;
    }

    [Fact]
    public void Apply_NullProxy_LeavesHandlerUntouched() {
        _output.WriteLine("[Scenario] ProxyConfigurator.Apply su un SocketsHttpHandler con httpClientOptions senza sezione httpProxy (null)");
        _output.WriteLine("[Atteso] Handler invariato: stessa istanza Proxy e stesso valore di UseProxy di prima");

        using var handler = new SocketsHttpHandler();
        var proxyBefore = handler.Proxy;
        var useProxyBefore = handler.UseProxy;

        ProxyConfigurator.Apply(handler, new httpClientOptions { Name = "A" });

        _output.WriteLine($"[Restituito] ProxyInvariato={ReferenceEquals(proxyBefore, handler.Proxy)}, UseProxy prima={useProxyBefore}, dopo={handler.UseProxy}");
        Assert.Same(proxyBefore, handler.Proxy);
        Assert.Equal(useProxyBefore, handler.UseProxy);
    }

    [Fact]
    public void Apply_UseProxyTrue_SetsWebProxyAndCredentials() {
        _output.WriteLine("[Scenario] ProxyConfigurator.Apply con httpProxy { Address=http://proxy.local:8080, UserName=user, Password=secret, UseProxy=true }");
        _output.WriteLine("[Atteso] Handler con UseProxy=true, WebProxy all'indirizzo configurato e NetworkCredential user/secret");

        using var handler = new SocketsHttpHandler();
        var options = new httpClientOptions {
            Name = "A",
            httpProxy = new httpProxy {
                Address = "http://proxy.local:8080",
                UserName = "user",
                Password = "secret",
                UseProxy = true
            }
        };

        ProxyConfigurator.Apply(handler, options);

        _output.WriteLine($"[Restituito] UseProxy={handler.UseProxy}, Proxy={handler.Proxy?.GetType().Name}, Address={(handler.Proxy as WebProxy)?.Address}, UserName={((handler.Proxy as WebProxy)?.Credentials as NetworkCredential)?.UserName}");
        Assert.True(handler.UseProxy);
        var proxy = Assert.IsType<WebProxy>(handler.Proxy);
        Assert.Equal(new Uri("http://proxy.local:8080"), proxy.Address);
        var credentials = Assert.IsType<NetworkCredential>(proxy.Credentials);
        Assert.Equal("user", credentials.UserName);
        Assert.Equal("secret", credentials.Password);
    }

    [Fact]
    public void Apply_InvalidAddress_LeavesProxyUnsetWithoutThrowing() {
        _output.WriteLine("[Scenario] ProxyConfigurator.Apply con httpProxy { Address=\"not a valid uri\", UseProxy=true }");
        _output.WriteLine("[Atteso] Nessuna eccezione: l'indirizzo non valido viene ignorato e handler.Proxy resta null");

        using var handler = new SocketsHttpHandler();
        var options = new httpClientOptions {
            Name = "A",
            httpProxy = new httpProxy { Address = "not a valid uri", UseProxy = true }
        };

        ProxyConfigurator.Apply(handler, options);

        _output.WriteLine($"[Restituito] Proxy={(handler.Proxy is null ? "null" : handler.Proxy.GetType().Name)}");
        Assert.Null(handler.Proxy);
    }
}
