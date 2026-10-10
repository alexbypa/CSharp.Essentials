using Xunit.Abstractions;
using System.Net;
using CSharpEssentials.HttpHelper.HttpMocks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace CSharpEssentials.HttpHelper.Tests;

public class HttpsClientHelperFactoryTests {
    private readonly ITestOutputHelper _output;

    public HttpsClientHelperFactoryTests(ITestOutputHelper output) {
        _output = output;
    }

    private static ServiceProvider BuildProvider(HttpStatusCode? mockStatus = null) {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> {
                ["HttpClientOptions:0:Name"] = "A",
                ["HttpClientOptions:1:Name"] = "B"
            })
            .Build();
        var services = new ServiceCollection();
        if (mockStatus is { } status)
            services.AddSingleton<IHttpMockScenario>(new HttpMockScenario(
                r => r.RequestUri?.Host == "mock.local",
                [() => Task.FromResult(new HttpResponseMessage(status))]));
        services.AddHttpClients(configuration);
        return services.BuildServiceProvider();
    }

    [Fact]
    public void CreateOrGet_KnownName_ReturnsHelper() {
        _output.WriteLine("[Scenario] Factory con client \"A\" e \"B\" configurati: CreateOrGet(\"A\")");
        _output.WriteLine("[Atteso] Viene restituito un httpsClientHelper");

        using var provider = BuildProvider();
        var factory = provider.GetRequiredService<IhttpsClientHelperFactory>();

        var helper = factory.CreateOrGet("A");
        _output.WriteLine($"[Restituito] Tipo={helper.GetType().Name}");

        Assert.IsType<httpsClientHelper>(helper);
    }

    [Fact]
    public void CreateOrGet_SameNameTwice_ReturnsCachedInstance() {
        _output.WriteLine("[Scenario] Richiedo due volte lo stesso nome (\"A\") al factory");
        _output.WriteLine("[Atteso] Entrambe le chiamate restituiscono la stessa istanza memorizzata in cache");

        using var provider = BuildProvider();
        var factory = provider.GetRequiredService<IhttpsClientHelperFactory>();

        var first = factory.CreateOrGet("A");
        var second = factory.CreateOrGet("A");

        _output.WriteLine($"[Restituito] StessaIstanza={ReferenceEquals(first, second)}");
        Assert.Same(first, second);
    }

    [Fact]
    public void CreateOrGet_DifferentNames_ReturnsDifferentInstances() {
        _output.WriteLine("[Scenario] Richiedo al factory due nomi diversi (\"A\" e \"B\")");
        _output.WriteLine("[Atteso] Vengono restituite istanze distinte e indipendenti");

        using var provider = BuildProvider();
        var factory = provider.GetRequiredService<IhttpsClientHelperFactory>();

        var a = factory.CreateOrGet("A");
        var b = factory.CreateOrGet("B");

        _output.WriteLine($"[Restituito] StessaIstanza={ReferenceEquals(a, b)}");
        Assert.NotSame(a, b);
    }

    [Fact]
    public void CreateOrGet_UnknownName_ThrowsArgumentException() {
        _output.WriteLine("[Scenario] CreateOrGet(\"missing\") per un nome non presente in configurazione");
        _output.WriteLine("[Atteso] ArgumentException con messaggio che contiene il nome richiesto (\"missing\")");

        using var provider = BuildProvider();
        var factory = provider.GetRequiredService<IhttpsClientHelperFactory>();

        var ex = Assert.Throws<ArgumentException>(() => factory.CreateOrGet("missing"));

        _output.WriteLine($"[Restituito] {ex.GetType().Name}: {ex.Message}");
        Assert.Contains("missing", ex.Message);
    }

    [Fact]
    public async Task CreateOrGet_WithMockScenario_SendAsyncReturnsMockedResponse() {
        _output.WriteLine("[Scenario] Scenario mock su mock.local che risponde 418; helper \"A\" invia GET http://mock.local/teapot");
        _output.WriteLine("[Atteso] La risposta è quella del mock: status 418, senza passare dalla rete reale");

        using var provider = BuildProvider((HttpStatusCode)418);
        var helper = provider.GetRequiredService<IhttpsClientHelperFactory>().CreateOrGet("A");

        using var response = await helper.SendAsync("http://mock.local/teapot", HttpMethod.Get);

        _output.WriteLine($"[Restituito] Status={(int)response.StatusCode}");
        Assert.Equal((HttpStatusCode)418, response.StatusCode);
    }

    // AddActionOnRequest(callback) registers a global callback: it fires for every client, in any call order.
    [Fact]
    public async Task AddActionOnRequest_AfterCreateOrGet_CallbackInvokedWithResponse() {
        _output.WriteLine("[Scenario] Helper \"A\" già creato, poi registro una callback globale con AddActionOnRequest; GET http://mock.local/ping (mock 200)");
        _output.WriteLine("[Atteso] La callback viene invocata con la richiesta (/ping), la stessa response restituita al chiamante e retry = 0");

        using var provider = BuildProvider(HttpStatusCode.OK);
        var factory = provider.GetRequiredService<IhttpsClientHelperFactory>();
        HttpRequestMessage? seenRequest = null;
        HttpResponseMessage? seenResponse = null;
        int? seenRetry = null;
        var helper = factory.CreateOrGet("A");
        factory.AddActionOnRequest((req, res, retry, _) => {
            seenRequest = req;
            seenResponse = res;
            seenRetry = retry;
            return Task.CompletedTask;
        });

        using var response = await helper.SendAsync("http://mock.local/ping", HttpMethod.Get);

        _output.WriteLine($"[Restituito] Status={(int)response.StatusCode}, PathVisto={seenRequest?.RequestUri?.AbsolutePath}, StessaResponse={ReferenceEquals(response, seenResponse)}, Retry={seenRetry}");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("/ping", seenRequest?.RequestUri?.AbsolutePath);
        Assert.Same(response, seenResponse);
        Assert.Equal(0, seenRetry);
    }

    [Fact]
    public async Task AddActionOnRequest_BeforeCreateOrGet_DoesNotThrowAndCallbackFires() {
        _output.WriteLine("[Scenario] Registro la callback globale con AddActionOnRequest prima di creare l'helper \"A\", poi GET http://mock.local/x");
        _output.WriteLine("[Atteso] Nessuna eccezione, il metodo non restituisce nulla (null) e la callback scatta una volta");

        using var provider = BuildProvider(HttpStatusCode.OK);
        var factory = provider.GetRequiredService<IhttpsClientHelperFactory>();
        var calls = 0;

        var returned = factory.AddActionOnRequest((_, _, _, _) => { calls++; return Task.CompletedTask; });
        using var response = await factory.CreateOrGet("A").SendAsync("http://mock.local/x", HttpMethod.Get);

        _output.WriteLine($"[Restituito] Ritorno={returned?.ToString() ?? "null"}, ChiamateCallback={calls}");
        Assert.Null(returned);
        Assert.Equal(1, calls);
    }

    [Fact]
    public async Task AddActionOnRequest_ForNamedClient_FiresOnlyForThatClient() {
        _output.WriteLine("[Scenario] Callback registrate per i client \"A\" e \"B\" separatamente; invio una richiesta solo con il client \"A\"");
        _output.WriteLine("[Atteso] Scatta solo la callback di \"A\" (1 volta) e non quella di \"B\" (0 volte)");

        using var provider = BuildProvider(HttpStatusCode.OK);
        var factory = provider.GetRequiredService<IhttpsClientHelperFactory>();
        var seenByA = 0;
        var seenByB = 0;
        factory.AddActionOnRequest("A", (_, _, _, _) => { seenByA++; return Task.CompletedTask; });
        factory.AddActionOnRequest("B", (_, _, _, _) => { seenByB++; return Task.CompletedTask; });

        using var response = await factory.CreateOrGet("A").SendAsync("http://mock.local/x", HttpMethod.Get);

        _output.WriteLine($"[Restituito] ChiamateA={seenByA}, ChiamateB={seenByB}");
        Assert.Equal(1, seenByA);
        Assert.Equal(0, seenByB);
    }

    [Fact]
    public async Task CreateOrGet_ConcurrentCallsSameName_ReturnSingleInstance() {
        _output.WriteLine("[Scenario] 32 chiamate concorrenti (Task.Run) a CreateOrGet(\"A\")");
        _output.WriteLine("[Atteso] Tutte ricevono la stessa istanza (nessuna race nella creazione)");

        using var provider = BuildProvider();
        var factory = provider.GetRequiredService<IhttpsClientHelperFactory>();

        var helpers = await Task.WhenAll(Enumerable.Range(0, 32).Select(_ => Task.Run(() => factory.CreateOrGet("A"))));

        _output.WriteLine($"[Restituito] IstanzeDistinte={helpers.Distinct().Count()} su {helpers.Length} chiamate");
        Assert.All(helpers, h => Assert.Same(helpers[0], h));
    }

    [Fact]
    public async Task HttpClientHandlerLogging_SameEventsAsGlobal_InvokesCallbackOnce() {
        _output.WriteLine("[Scenario] HttpClientHandlerLogging costruito con lo stesso HttpRequestEvents sia come eventi globali sia come eventi del client; una GET che risponde 200");
        _output.WriteLine("[Atteso] La callback viene invocata una sola volta (nessun doppio invio quando le due sorgenti coincidono)");

        var events = new HttpRequestEvents();
        var calls = 0;
        events.Add((_, _, _, _) => { calls++; return Task.CompletedTask; });
        using var invoker = new HttpMessageInvoker(new HttpClientHandlerLogging(events, events) { InnerHandler = new OkHandler() });

        using var response = await invoker.SendAsync(new HttpRequestMessage(HttpMethod.Get, "http://x.local/"), default);

        _output.WriteLine($"[Restituito] ChiamateCallback={calls}");
        Assert.Equal(1, calls);
    }

    [Fact]
    public async Task HttpClientHandlerLogging_WithRequestContent_DoesNotReadBody() {
        _output.WriteLine("[Scenario] POST con contenuto che passa da HttpClientHandlerLogging verso un handler interno che non legge il body");
        _output.WriteLine("[Atteso] Il body non viene mai serializzato dall'handler di logging (nessun buffering inutile)");

        var content = new CountingContent();
        using var invoker = new HttpMessageInvoker(new HttpClientHandlerLogging(new HttpRequestEvents()) { InnerHandler = new OkHandler() });

        using var response = await invoker.SendAsync(new HttpRequestMessage(HttpMethod.Post, "http://x.local/") { Content = content }, default);

        _output.WriteLine($"[Restituito] Serializzazioni={content.Serializations}");
        Assert.Equal(0, content.Serializations);
    }

    private sealed class CountingContent : HttpContent {
        public int Serializations { get; private set; }
        protected override Task SerializeToStreamAsync(Stream stream, TransportContext? context) {
            Serializations++;
            return Task.CompletedTask;
        }
        protected override bool TryComputeLength(out long length) {
            length = 0;
            return true;
        }
    }

    private sealed class OkHandler : HttpMessageHandler {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK));
    }

    [Fact]
    public async Task HttpRequestEvents_ConcurrentAddAndInvoke_DoesNotThrow() {
        _output.WriteLine("[Scenario] 64 task concorrenti che aggiungono una callback a HttpRequestEvents e invocano InvokeAll nello stesso momento");
        _output.WriteLine("[Atteso] Nessuna eccezione (es. \"Collection was modified\"): la collezione di callback è thread-safe");

        var events = new HttpRequestEvents();
        using var req = new HttpRequestMessage();
        using var res = new HttpResponseMessage();

        await Task.WhenAll(Enumerable.Range(0, 64).Select(_ => Task.Run(async () => {
            events.Add((_, _, _, _) => Task.CompletedTask);
            await events.InvokeAll(req, res, 0, TimeSpan.Zero);
        })));
        _output.WriteLine($"[Restituito] Tutti i 64 task sono terminati senza eccezioni");
    }

    [Fact]
    public async Task HttpRequestEvents_CallbackThrows_DoesNotThrowAndRunsOthers() {
        _output.WriteLine("[Scenario] Tre callback registrate: una lancia in modo sincrono, una lancia in modo asincrono, una è corretta; InvokeAll");
        _output.WriteLine("[Atteso] Nessuna eccezione propagata e tutte e tre le callback vengono eseguite");

        var events = new HttpRequestEvents();
        var syncThrowRan = false;
        var asyncThrowRan = false;
        var okRan = false;
        events.Add((_, _, _, _) => { syncThrowRan = true; throw new InvalidOperationException("sync"); });
        events.Add(async (_, _, _, _) => { asyncThrowRan = true; await Task.Yield(); throw new InvalidOperationException("async"); });
        events.Add((_, _, _, _) => { okRan = true; return Task.CompletedTask; });
        using var req = new HttpRequestMessage();
        using var res = new HttpResponseMessage();

        await events.InvokeAll(req, res, 0, TimeSpan.Zero);

        _output.WriteLine($"[Restituito] Sincrona={syncThrowRan}, Asincrona={asyncThrowRan}, Corretta={okRan}");
        Assert.True(syncThrowRan && asyncThrowRan && okRan);
    }

    [Fact]
    public async Task HttpRequestEvents_CallbackCancelled_PropagatesCancellation() {
        _output.WriteLine("[Scenario] Callback che restituisce un Task già cancellato; InvokeAll");
        _output.WriteLine("[Atteso] La cancellazione non viene inghiottita: viene propagata come OperationCanceledException");

        var events = new HttpRequestEvents();
        events.Add((_, _, _, _) => Task.FromCanceled(new CancellationToken(true)));
        using var req = new HttpRequestMessage();
        using var res = new HttpResponseMessage();

        var ex = await Assert.ThrowsAnyAsync<OperationCanceledException>(() => events.InvokeAll(req, res, 0, TimeSpan.Zero));
        _output.WriteLine($"[Restituito] {ex.GetType().Name}");
    }
}
