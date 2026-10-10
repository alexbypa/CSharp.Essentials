using Xunit.Abstractions;
using CSharpEssentials.HttpHelper.HttpMocks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace CSharpEssentials.HttpHelper.Tests;

public class HttpExtensionTests {
    private readonly ITestOutputHelper _output;

    public HttpExtensionTests(ITestOutputHelper output) {
        _output = output;
    }

    private static IConfiguration BuildConfiguration(Dictionary<string, string?>? values = null) =>
        new ConfigurationBuilder()
            .AddInMemoryCollection(values ?? new Dictionary<string, string?> {
                ["HttpClientOptions:0:Name"] = "A",
                ["HttpClientOptions:0:UseCompression"] = "true",
                ["HttpClientOptions:0:RateLimitOptions:PermitLimit"] = "5",
                ["HttpClientOptions:1:Name"] = "B"
            })
            .Build();

    [Fact]
    public void AddHttpClients_Always_RegistersCoreServicesWithExpectedLifetimes() {
        _output.WriteLine("🚀 [Scenario] Test registrazione servizi base di HttpClients con i lifetime attesi");

        var services = new ServiceCollection();
        var config = BuildConfiguration();
        _output.WriteLine("⏱️ [Parametri] ServiceCollection vuota, configurazione di default");

        services.AddHttpClients(config);
        
        _output.WriteLine("🎯 [Atteso] Lifetime Singleton per IhttpsClientHelperFactory, IHttpRequestEvents, IHttpMockEngine");
        _output.WriteLine("🎯 [Atteso] Lifetime Transient per HttpClientHandlerLogging, HttpMockDelegatingHandler");

        AssertLifetime<IhttpsClientHelperFactory>(services, ServiceLifetime.Singleton);
        AssertLifetime<IHttpRequestEvents>(services, ServiceLifetime.Singleton);
        AssertLifetime<IHttpMockEngine>(services, ServiceLifetime.Singleton);
        AssertLifetime<HttpClientHandlerLogging>(services, ServiceLifetime.Transient);
        AssertLifetime<HttpMockDelegatingHandler>(services, ServiceLifetime.Transient);
        
        _output.WriteLine("📦 [Restituito] Tutti i servizi sono stati registrati con i lifetime corretti");
    }

    [Fact]
    public void AddHttpClients_Always_BindsHttpHelperLoggingOptions() {
        _output.WriteLine("[Scenario] Configurazione con HttpHelperLogging:LogRequests = [A, *]");
        _output.WriteLine("[Atteso] IOptionsMonitor<HttpHelperLoggingOptions> espone LogRequests = [A, *]");

        var config = BuildConfiguration(new Dictionary<string, string?> {
            ["HttpClientOptions:0:Name"] = "A",
            ["HttpHelperLogging:LogRequests:0"] = "A",
            ["HttpHelperLogging:LogRequests:1"] = "*"
        });
        var services = new ServiceCollection();
        services.AddHttpClients(config);
        using var provider = services.BuildServiceProvider();

        var logRequests = provider.GetRequiredService<IOptionsMonitor<HttpHelperLoggingOptions>>().CurrentValue.LogRequests;

        _output.WriteLine($"[Restituito] {string.Join(",", logRequests)}");
        Assert.Equal(["A", "*"], logRequests);
    }

    [Fact]
    public void AddHttpClients_AnyConfiguration_ReturnsSameServiceCollection() {
        _output.WriteLine("🚀 [Scenario] Test AddHttpClients restituisce la stessa istanza di ServiceCollection (fluent API)");
        
        var services = new ServiceCollection();
        var config = BuildConfiguration();

        var result = services.AddHttpClients(config);

        _output.WriteLine($"🎯 [Atteso] Risultato: {services.GetHashCode()}");
        _output.WriteLine($"📦 [Restituito] Risultato: {result.GetHashCode()}");
        Assert.Same(services, result);
    }

    [Fact]
    public void AddHttpClients_ConfiguredClients_BindsOptionsList() {
        _output.WriteLine("🚀 [Scenario] Test binding della configurazione di HttpClients nella OptionsList");
        var services = new ServiceCollection();
        services.AddHttpClients(BuildConfiguration());
        using var provider = services.BuildServiceProvider();

        var options = provider.GetRequiredService<IOptions<List<httpClientOptions>>>().Value;

        _output.WriteLine($"🎯 [Atteso] Count: 2, Client1 Name: A, Client2 Name: B");
        _output.WriteLine($"📦 [Restituito] Count: {options.Count}, Client1 Name: {options[0].Name}, Client2 Name: {options[1].Name}");

        Assert.Equal(2, options.Count);
        Assert.Equal("A", options[0].Name);
        Assert.True(options[0].UseCompression);
        Assert.Equal(5, options[0].RateLimitOptions!.PermitLimit);
        Assert.Equal("B", options[1].Name);
        Assert.False(options[1].UseCompression);
    }

    [Fact]
    public void CreateOrGet_NoConfiguredClients_ThrowsArgumentException() {
        _output.WriteLine("[Scenario] Nessun client in configurazione (HttpClientOptions vuoto): factory.CreateOrGet(\"x\")");
        _output.WriteLine("[Atteso] ArgumentException: il client \"x\" non esiste");

        var services = new ServiceCollection();
        services.AddHttpClients(BuildConfiguration(new Dictionary<string, string?>()));
        using var provider = services.BuildServiceProvider();

        var factory = provider.GetRequiredService<IhttpsClientHelperFactory>();

        var ex = Assert.Throws<ArgumentException>(() => factory.CreateOrGet("x"));
        _output.WriteLine($"[Restituito] {ex.GetType().Name}: {ex.Message}");
    }

    [Fact]
    public void AddHttpClients_NoConfiguredClients_StillRegistersHttpClientFactory() {
        _output.WriteLine("[Scenario] AddHttpClients con configurazione vuota (nessun client definito): risolvo IHttpClientFactory");
        _output.WriteLine("[Atteso] IHttpClientFactory è comunque registrato (non null)");

        var services = new ServiceCollection();
        services.AddHttpClients(BuildConfiguration(new Dictionary<string, string?>()));
        using var provider = services.BuildServiceProvider();

        _output.WriteLine($"[Restituito] IHttpClientFactory={(provider.GetService<IHttpClientFactory>() is null ? "null" : "registrato")}");
        Assert.NotNull(provider.GetService<IHttpClientFactory>());
    }

    [Fact]
    public void AddHttpClients_Always_WritesNothingToConsole() {
        _output.WriteLine("[Scenario] Console.Out reindirizzata su uno StringWriter durante AddHttpClients con la configurazione di default");
        _output.WriteLine("[Atteso] Nessun output su console (la libreria non deve scrivere con Console.WriteLine)");

        var original = Console.Out;
        var captured = new StringWriter();
        Console.SetOut(captured);
        try {
            new ServiceCollection().AddHttpClients(BuildConfiguration());
        } finally {
            Console.SetOut(original);
        }

        _output.WriteLine($"[Restituito] ConsoleOutput=\"{captured}\"");
        Assert.Equal(string.Empty, captured.ToString());
    }

    [Fact]
    public void RequestHttpExtension_IdTransaction_IsUniquePerCall() {
        _output.WriteLine("[Scenario] Leggo due volte IdTransaction dalla stessa istanza di RequestHttpExtension");
        _output.WriteLine("[Atteso] Ogni lettura genera un nuovo identificativo: i due valori sono diversi");

        var request = new RequestHttpExtension();

        var first = request.IdTransaction;
        var second = request.IdTransaction;
        _output.WriteLine($"[Restituito] Prima={first}, Seconda={second}");

        Assert.NotEqual(first, second);
    }

    [Fact]
    public void CertificateConfigurator_Apply_MissingFile_LeavesHandlerWithoutClientCertificates() {
        _output.WriteLine("[Scenario] CertificateConfigurator.Apply con un certificato il cui file .pfx non esiste");
        _output.WriteLine("[Atteso] Nessuna eccezione: il handler resta senza ClientCertificates e senza RemoteCertificateValidationCallback");

        using var handler = new SocketsHttpHandler();
        var options = new httpClientOptions {
            Name = "A",
            Certificate = new httpClientCertificate { Path = Path.Combine(Path.GetTempPath(), "does-not-exist.pfx"), Password = "x" }
        };

        CertificateConfigurator.Apply(handler, options);

        _output.WriteLine($"[Restituito] ClientCertificates={(handler.SslOptions.ClientCertificates is null ? "null" : "valorizzato")}, ValidationCallback={(handler.SslOptions.RemoteCertificateValidationCallback is null ? "null" : "valorizzato")}");
        Assert.Null(handler.SslOptions.ClientCertificates);
        Assert.Null(handler.SslOptions.RemoteCertificateValidationCallback);
    }

    [Fact]
    public void CertificateConfigurator_Apply_PasswordWithoutPath_LeavesHandlerWithoutClientCertificates() {
        _output.WriteLine("[Scenario] CertificateConfigurator.Apply con httpClientCertificate che ha Password ma nessun Path");
        _output.WriteLine("[Atteso] Nessuna eccezione: senza Path non si carica alcun certificato (ClientCertificates null)");

        using var handler = new SocketsHttpHandler();
        var options = new httpClientOptions { Name = "A", Certificate = new httpClientCertificate { Password = "x" } };

        CertificateConfigurator.Apply(handler, options);

        _output.WriteLine($"[Restituito] ClientCertificates={(handler.SslOptions.ClientCertificates is null ? "null" : "valorizzato")}");
        Assert.Null(handler.SslOptions.ClientCertificates);
    }

    [Fact]
    public void AddHttpClients_ConfiguredClient_IHttpClientFactoryCreatesNamedClient() {
        _output.WriteLine("[Scenario] AddHttpClients con i client \"A\" e \"B\" configurati: IHttpClientFactory.CreateClient(\"A\")");
        _output.WriteLine("[Atteso] Viene creato un HttpClient non null");

        var services = new ServiceCollection();
        services.AddHttpClients(BuildConfiguration());
        using var provider = services.BuildServiceProvider();

        using var client = provider.GetRequiredService<IHttpClientFactory>().CreateClient("A");

        _output.WriteLine($"[Restituito] Client={(client is null ? "null" : client.GetType().Name)}");
        Assert.NotNull(client);
    }

    private static void AssertLifetime<TService>(IServiceCollection services, ServiceLifetime expected) {
        var descriptor = Assert.Single(services, d => d.ServiceType == typeof(TService));
        Assert.Equal(expected, descriptor.Lifetime);
    }
}
