using Xunit.Abstractions;
using CSharpEssentials.HttpHelper.HttpMocks;

namespace CSharpEssentials.HttpHelper.Tests;

public class HttpMockEngineTests {
    private readonly ITestOutputHelper _output;

    public HttpMockEngineTests(ITestOutputHelper output) {
        _output = output;
    }

    private static Func<Task<HttpResponseMessage>> Respond(string body) =>
        () => Task.FromResult(new HttpResponseMessage { Content = new StringContent(body) });

    private static HttpMockScenario Scenario(Func<HttpRequestMessage, bool> match, params string[] bodies) =>
        new(match, bodies.Select(Respond).ToList());

    private static async Task<string> Send(HttpMessageHandler handler, string url = "http://x/a") {
        using var invoker = new HttpMessageInvoker(handler, disposeHandler: false);
        using var response = await invoker.SendAsync(new HttpRequestMessage(HttpMethod.Get, url), CancellationToken.None);
        return await response.Content.ReadAsStringAsync();
    }

    [Fact]
    public async Task Build_CyclesResponseFactoriesRoundRobin() {
        _output.WriteLine("[Scenario] Mock che risponde con due factory (\"1\", \"2\") sempre abbinate: 3 richieste GET consecutive");
        _output.WriteLine("[Atteso] Le risposte ruotano in round-robin: [\"1\", \"2\", \"1\"]");

        var handler = new HttpMockEngine([Scenario(_ => true, "1", "2")]).Build();
        var bodies = new[] { await Send(handler), await Send(handler), await Send(handler) };
        _output.WriteLine($"[Restituito] Bodies=[{string.Join(", ", bodies)}]");

        Assert.Equal(["1", "2", "1"], bodies);
    }

    [Fact]
    public async Task Build_MultipleMatches_LastScenarioWins() {
        _output.WriteLine("[Scenario] Due scenari che corrispondono entrambi alla richiesta (\"first\" e \"last\")");
        _output.WriteLine("[Atteso] Vince l'ultimo scenario registrato: body \"last\"");

        var handler = new HttpMockEngine([Scenario(_ => true, "first"), Scenario(_ => true, "last")]).Build();
        var body = await Send(handler);
        _output.WriteLine($"[Restituito] Body=\"{body}\"");

        Assert.Equal("last", body);
    }

    [Fact]
    public async Task Build_NoMatch_ThrowsInvalidOperation() {
        _output.WriteLine("[Scenario] Scenario che risponde solo a /a, ma la richiesta è GET /b");
        _output.WriteLine("[Atteso] InvalidOperationException: nessuno scenario corrisponde (niente fallback silenzioso)");

        var handler = new HttpMockEngine([Scenario(r => r.RequestUri!.AbsolutePath == "/a", "a")]).Build();
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => Send(handler, "http://x/b"));
        _output.WriteLine($"[Restituito] {ex.GetType().Name}: {ex.Message}");
    }

    [Fact]
    public void Build_CachesHandler() {
        _output.WriteLine("[Scenario] Chiamo Build() due volte sullo stesso HttpMockEngine");
        _output.WriteLine("[Atteso] Viene restituita la stessa istanza dell'handler (cache)");

        var engine = new HttpMockEngine([Scenario(_ => true, "1")]);
        var first = engine.Build();
        var second = engine.Build();
        _output.WriteLine($"[Restituito] StessaIstanza={ReferenceEquals(first, second)}");

        Assert.Same(first, second);
    }

    [Fact]
    public async Task Build_ConcurrentRequests_CursorIsNotLost() {
        _output.WriteLine("[Scenario] 200 richieste concorrenti (Task.Run) verso uno scenario con 2 risposte (\"a\", \"b\")");
        _output.WriteLine("[Atteso] Il cursore round-robin è thread-safe: esattamente 100 risposte \"a\" (e 100 \"b\")");

        var handler = new HttpMockEngine([Scenario(_ => true, "a", "b")]).Build();
        var bodies = await Task.WhenAll(Enumerable.Range(0, 200).Select(_ => Task.Run(() => Send(handler))));
        _output.WriteLine($"[Restituito] Risposte \"a\"={bodies.Count(b => b == "a")}, \"b\"={bodies.Count(b => b == "b")}");
        Assert.Equal(100, bodies.Count(b => b == "a"));
    }

    [Fact]
    public async Task Build_EmptyResponseFactory_ThrowsInvalidOperation() {
        _output.WriteLine("[Scenario] Scenario che corrisponde ma senza alcuna response factory");
        _output.WriteLine("[Atteso] InvalidOperationException: lo scenario è mal configurato");

        var handler = new HttpMockEngine([Scenario(_ => true)]).Build();
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => Send(handler));
        _output.WriteLine($"[Restituito] {ex.GetType().Name}: {ex.Message}");
    }

    [Fact]
    public async Task Build_RequestAwareFactory_ReceivesRequestAndToken() {
        _output.WriteLine("[Scenario] Scenario con 2 factory \"request-aware\" (ricevono HttpRequestMessage e CancellationToken): 3 richieste con un token cancellabile");
        _output.WriteLine("[Atteso] Risposte in round-robin [\"1\", \"2\", \"1\"]; a ogni factory arriva la stessa richiesta inviata e un token cancellabile");

        var seen = new List<(HttpRequestMessage Request, CancellationToken Token)>();
        Func<string, Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>>> make = body => (r, ct) => {
            seen.Add((r, ct));
            return Task.FromResult(new HttpResponseMessage { Content = new StringContent(body) });
        };
        var handler = new HttpMockEngine([new HttpMockScenario(_ => true, [make("1"), make("2")])]).Build();
        using var cts = new CancellationTokenSource();
        using var invoker = new HttpMessageInvoker(handler, disposeHandler: false);
        var bodies = new List<string>();
        var requests = new List<HttpRequestMessage>();
        for (var i = 0; i < 3; i++) {
            var request = new HttpRequestMessage(HttpMethod.Get, "http://x/a");
            requests.Add(request);
            using var response = await invoker.SendAsync(request, cts.Token);
            bodies.Add(await response.Content.ReadAsStringAsync());
        }

        _output.WriteLine($"[Restituito] Bodies=[{string.Join(", ", bodies)}], ChiamateFactory={seen.Count}");
        Assert.Equal(["1", "2", "1"], bodies);
        for (var i = 0; i < 3; i++) {
            Assert.Same(requests[i], seen[i].Request);
            Assert.True(seen[i].Token.CanBeCanceled);
        }
    }

    [Fact]
    public async Task Build_RequestAwareScenarioEmpty_ThrowsInvalidOperation() {
        _output.WriteLine("[Scenario] Scenario \"request-aware\" con lista di factory vuota");
        _output.WriteLine("[Atteso] InvalidOperationException: lo scenario è mal configurato");

        var handler = new HttpMockEngine([new HttpMockScenario(_ => true, new List<Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>>>())]).Build();
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => Send(handler));
        _output.WriteLine($"[Restituito] {ex.GetType().Name}: {ex.Message}");
    }

    [Fact]
    public async Task DelegatingHandler_MockConfigurationError_DoesNotFallBackToNetwork() {
        _output.WriteLine("[Scenario] HttpMockDelegatingHandler con scenario senza factory e InnerHandler che simula la rete reale (lancia se raggiunto)");
        _output.WriteLine("[Atteso] InvalidOperationException di configurazione del mock; la rete reale non viene mai raggiunta");

        var engine = new HttpMockEngine([Scenario(_ => true)]);
        var handler = new HttpMockDelegatingHandler(engine) { InnerHandler = new ThrowingHandler() };
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => Send(handler));
        _output.WriteLine($"[Restituito] {ex.GetType().Name}: {ex.Message}");
    }

    private sealed class ThrowingHandler : HttpMessageHandler {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
            throw new Xunit.Sdk.XunitException("real network reached");
    }
}
