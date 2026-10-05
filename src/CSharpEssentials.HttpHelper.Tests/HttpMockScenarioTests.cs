using System.Net;
using CSharpEssentials.HttpHelper.HttpMocks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Xunit.Abstractions;

namespace CSharpEssentials.HttpHelper.Tests;

/// <summary>
/// End-to-end mock scenarios: DI → <see cref="IhttpsClientHelperFactory"/> → helper (retry/timeout) → <see cref="HttpMockDelegatingHandler"/> → <see cref="HttpMockEngine"/>.
/// Each test writes "[Scenario] ... / [Atteso] ... / [Restituito] ..." to the test output (Test Explorer or <c>dotnet test --logger "console;verbosity=detailed"</c>).
/// </summary>
public class HttpMockScenarioTests(ITestOutputHelper output) {
    private const string Host = "mock.local";

    private void Got(HttpResponseMessage response, CallCounter? calls = null) =>
        output.WriteLine($"[Restituito] {(int)response.StatusCode} {response.StatusCode}" +
            (calls is null ? "" : $" after {calls.Value} mock call(s)"));

    private static ServiceProvider BuildProvider(params IHttpMockScenario[] scenarios) {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["HttpClientOptions:0:Name"] = "A" })
            .Build();
        var services = new ServiceCollection();
        foreach (var scenario in scenarios)
            services.AddSingleton(scenario);
        services.AddHttpClients(configuration);
        return services.BuildServiceProvider();
    }

    private static IhttpsClientHelper Helper(ServiceProvider provider) =>
        provider.GetRequiredService<IhttpsClientHelperFactory>().CreateOrGet("A");

    /// <summary>Scenario for <paramref name="path"/> on the mock host; <paramref name="calls"/> counts how many responses were served.</summary>
    private HttpMockScenario Scenario(string path, CallCounter calls, params Func<HttpResponseMessage>[] responses) =>
        new(r => r.RequestUri?.Host == Host && r.RequestUri.AbsolutePath == path,
            responses.Select(build => (Func<Task<HttpResponseMessage>>)(() => {
                var call = calls.Increment();
                var response = build();
                output.WriteLine($"  mock call {call} on {path} -> {(int)response.StatusCode} {response.StatusCode}");
                return Task.FromResult(response);
            })).ToList());

    private static Func<HttpResponseMessage> Status(HttpStatusCode code) => () => new HttpResponseMessage(code);

    private sealed class CallCounter {
        private int _value;
        public int Value => _value;
        public int Increment() => Interlocked.Increment(ref _value);
    }

    [Fact]
    public async Task TooManyRequestsTwiceThenOk_WithRetryOn429_ReturnsOkOnThirdAttempt() {
        output.WriteLine("[Scenario] Mock su /r che risponde 429, 429 e poi 200; helper con retry sullo status 429 (3 retry, backoff 0)");
        output.WriteLine("[Atteso] Alla terza chiamata arriva 200 OK: il mock è stato chiamato 3 volte");

        var calls = new CallCounter();
        using var provider = BuildProvider(Scenario("/r", calls,
            Status(HttpStatusCode.TooManyRequests), Status(HttpStatusCode.TooManyRequests), Status(HttpStatusCode.OK)));
        var helper = Helper(provider).addRetryCondition(r => r.StatusCode == HttpStatusCode.TooManyRequests, 3, 0);

        using var response = await helper.SendAsync($"http://{Host}/r", HttpMethod.Get);
        Got(response, calls);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(3, calls.Value);
    }

    [Fact]
    public async Task TooManyRequestsWithRetryAfter_WithRetryOn429_RetriesAndSucceeds() {
        output.WriteLine("[Scenario] Mock su /r che risponde 429 con header Retry-After di 1 ora e poi 200; helper con retry su 429 (1 retry, backoff 0)");
        output.WriteLine("[Atteso] Il retry avviene subito (l'helper usa il proprio backoff, non rispetta Retry-After): 200 OK dopo 2 chiamate");

        // The helper uses its own backoff (backoffFactor^attempt seconds); Retry-After is not honoured.
        var calls = new CallCounter();
        using var provider = BuildProvider(Scenario("/r", calls,
            () => new HttpResponseMessage(HttpStatusCode.TooManyRequests) { Headers = { RetryAfter = new(TimeSpan.FromHours(1)) } },
            Status(HttpStatusCode.OK)));
        var helper = Helper(provider).addRetryCondition(r => r.StatusCode == HttpStatusCode.TooManyRequests, 1, 0);

        using var response = await helper.SendAsync($"http://{Host}/r", HttpMethod.Get);
        Got(response, calls);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(2, calls.Value);
    }

    [Fact]
    public async Task Forbidden_WithRetryOnlyOn429_ReturnsForbiddenWithoutRetry() {
        output.WriteLine("[Scenario] Mock su /f che risponde 403 e poi 200; helper con retry solo su 429");
        output.WriteLine("[Atteso] 403 Forbidden restituito senza alcun retry: il mock è chiamato una sola volta");

        var calls = new CallCounter();
        using var provider = BuildProvider(Scenario("/f", calls, Status(HttpStatusCode.Forbidden), Status(HttpStatusCode.OK)));
        var helper = Helper(provider).addRetryCondition(r => r.StatusCode == HttpStatusCode.TooManyRequests, 3, 0);

        using var response = await helper.SendAsync($"http://{Host}/f", HttpMethod.Get);
        Got(response, calls);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        Assert.Equal(1, calls.Value);
    }

    [Fact]
    public async Task PersistentServerError_RetryExhausted_ReturnsLastErrorAfterAllAttempts() {
        output.WriteLine("[Scenario] Mock su /e che risponde sempre 500; helper con retry su 5xx (2 retry, backoff 0)");
        output.WriteLine("[Atteso] Retry esauriti: viene restituito l'ultimo 500 dopo 3 chiamate totali (1 + 2 retry)");

        var calls = new CallCounter();
        using var provider = BuildProvider(Scenario("/e", calls, Status(HttpStatusCode.InternalServerError)));
        var helper = Helper(provider).addRetryCondition(r => (int)r.StatusCode >= 500, 2, 0);

        using var response = await helper.SendAsync($"http://{Host}/e", HttpMethod.Get);
        Got(response, calls);

        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
        Assert.Equal(3, calls.Value);
    }

    [Fact]
    public async Task DifferentPaths_RouteToTheirOwnScenario() {
        output.WriteLine("[Scenario] Due scenari sullo stesso host: /a risponde 200 e /b risponde 403; chiamo entrambi i path");
        output.WriteLine("[Atteso] Ogni path usa il proprio scenario: /a → 200 (1 chiamata), /b → 403 (1 chiamata)");

        var okCalls = new CallCounter();
        var forbiddenCalls = new CallCounter();
        using var provider = BuildProvider(
            Scenario("/a", okCalls, Status(HttpStatusCode.OK)),
            Scenario("/b", forbiddenCalls, Status(HttpStatusCode.Forbidden)));
        var helper = Helper(provider);

        using var a = await helper.SendAsync($"http://{Host}/a", HttpMethod.Get);
        using var b = await helper.SendAsync($"http://{Host}/b", HttpMethod.Get);
        Got(a);
        Got(b);

        Assert.Equal(HttpStatusCode.OK, a.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, b.StatusCode);
        Assert.Equal(1, okCalls.Value);
        Assert.Equal(1, forbiddenCalls.Value);
    }

    [Fact]
    public async Task MockedBodyAndHeaders_ReachTheCaller() {
        output.WriteLine("[Scenario] Mock su /j che risponde 401 con body JSON {\"error\":\"invalid_token\"} e header X-Request-Id=abc-123");
        output.WriteLine("[Atteso] Status, body, Content-Type (application/json) e header arrivano intatti al chiamante");

        using var provider = BuildProvider(Scenario("/j", new CallCounter(), () => {
            var response = new HttpResponseMessage(HttpStatusCode.Unauthorized) {
                Content = new StringContent("{\"error\":\"invalid_token\"}", System.Text.Encoding.UTF8, "application/json")
            };
            response.Headers.Add("X-Request-Id", "abc-123");
            return response;
        }));

        using var response = await Helper(provider).SendAsync($"http://{Host}/j", HttpMethod.Get);
        Got(response);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Equal("{\"error\":\"invalid_token\"}", await response.Content.ReadAsStringAsync());
        Assert.Equal("application/json", response.Content.Headers.ContentType?.MediaType);
        Assert.Equal("abc-123", Assert.Single(response.Headers.GetValues("X-Request-Id")));
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task ScenarioThrowsHttpRequestException_Returns502WithoutRealNetwork(bool throwSynchronously) {
        output.WriteLine($"[Scenario] Lo scenario mock lancia HttpRequestException(\"simulated network failure\") ({(throwSynchronously ? "in modo sincrono" : "come Task fallito")})");
        output.WriteLine("[Atteso] L'helper converte l'errore in 502 Bad Gateway con il messaggio simulato nel body (la rete reale non viene toccata)");

        Func<Task<HttpResponseMessage>> fail = throwSynchronously
            ? () => throw new HttpRequestException("simulated network failure")
            : () => Task.FromException<HttpResponseMessage>(new HttpRequestException("simulated network failure"));
        using var provider = BuildProvider(new HttpMockScenario(r => r.RequestUri?.Host == Host, [fail]));

        using var response = await Helper(provider).SendAsync($"http://{Host}/n", HttpMethod.Get);
        Got(response);

        // A real-network fallback would fail on DNS with a different message.
        var body = await response.Content.ReadAsStringAsync();
        output.WriteLine($"Body: {body}");
        Assert.Equal(HttpStatusCode.BadGateway, response.StatusCode);
        Assert.Contains("simulated network failure", body);
    }

    [Fact]
    public async Task NetworkFailureThenOk_WithRetryOn502_ReturnsOk() {
        output.WriteLine("[Scenario] Mock che alla prima chiamata lancia HttpRequestException e alla seconda risponde 200; helper con retry su 502 (2 retry, backoff 0)");
        output.WriteLine("[Atteso] Il primo errore di rete diventa 502, viene ritentato e la seconda chiamata restituisce 200 OK (2 chiamate al mock)");

        var calls = new CallCounter();
        var scenario = new HttpMockScenario(r => r.RequestUri?.Host == Host, [
            () => { output.WriteLine($"  mock call {calls.Increment()} -> throws HttpRequestException"); throw new HttpRequestException("simulated network failure"); },
            () => { output.WriteLine($"  mock call {calls.Increment()} -> 200 OK"); return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)); }
        ]);
        using var provider = BuildProvider(scenario);
        var helper = Helper(provider).addRetryCondition(r => r.StatusCode == HttpStatusCode.BadGateway, 2, 0);

        using var response = await helper.SendAsync($"http://{Host}/n", HttpMethod.Get);
        Got(response, calls);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(2, calls.Value);
    }

    [Fact]
    public async Task DelegatingHandler_ScenarioThrows_DoesNotFallBackToNetwork() {
        output.WriteLine("[Scenario] HttpMockDelegatingHandler da solo: lo scenario lancia HttpRequestException(\"simulated\") e l'InnerHandler simula la rete reale (lancia se raggiunto)");
        output.WriteLine("[Atteso] Viene rilanciata HttpRequestException(\"simulated\") dallo scenario; la rete reale non viene raggiunta");

        var engine = new HttpMockEngine([new HttpMockScenario(_ => true, [() => throw new HttpRequestException("simulated")])]);
        var handler = new HttpMockDelegatingHandler(engine) { InnerHandler = new ThrowingHandler() };
        using var invoker = new HttpMessageInvoker(handler);

        var ex = await Assert.ThrowsAsync<HttpRequestException>(() =>
            invoker.SendAsync(new HttpRequestMessage(HttpMethod.Get, "http://x/a"), CancellationToken.None));
        output.WriteLine($"[Restituito] {ex.GetType().Name} \"{ex.Message}\" (real network not reached)");

        Assert.Equal("simulated", ex.Message);
    }

    [Fact]
    public async Task SlowMockWithToken_WithAddTimeout_Returns408() {
        output.WriteLine("[Scenario] Mock che attende all'infinito sul proprio CancellationToken; helper con addTimeout di 100 ms");
        output.WriteLine("[Atteso] 408 Request Timeout con \"timeout\" nel body, entro 5 s, e la factory osserva il token cancellato (il mock non resta appeso)");

        var tokenCancelled = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var scenario = new HttpMockScenario(r => r.RequestUri?.Host == Host, [
            async (r, ct) => {
                try { await Task.Delay(Timeout.Infinite, ct); }
                catch (OperationCanceledException) { tokenCancelled.TrySetResult(); throw; }
                return new HttpResponseMessage(HttpStatusCode.OK);
            }
        ]);
        using var provider = BuildProvider(scenario);
        var helper = Helper(provider).addTimeout(TimeSpan.FromMilliseconds(100));

        var started = DateTime.UtcNow;
        using var response = await helper.SendAsync($"http://{Host}/slow", HttpMethod.Get);
        var elapsed = DateTime.UtcNow - started;
        Got(response);
        var body = await response.Content.ReadAsStringAsync();
        output.WriteLine($"Body: {body} (elapsed {elapsed.TotalMilliseconds:F0} ms)");

        Assert.Equal(HttpStatusCode.RequestTimeout, response.StatusCode);
        Assert.Contains("timeout", body);
        await tokenCancelled.Task.WaitAsync(TimeSpan.FromSeconds(5)); // factory must observe the cancelled token
        Assert.True(elapsed < TimeSpan.FromSeconds(5), $"took {elapsed}");
    }

    [Fact]
    public async Task SlowMock_CallerCancels_ThrowsOperationCanceled() {
        output.WriteLine("[Scenario] Mock lento che attende il token; il chiamante cancella il proprio CancellationToken dopo 100 ms");
        output.WriteLine("[Atteso] OperationCanceledException propagata al chiamante (nessun 408 mascherato)");

        var scenario = new HttpMockScenario(r => r.RequestUri?.Host == Host, [
            async (r, ct) => { await Task.Delay(Timeout.Infinite, ct); return new HttpResponseMessage(HttpStatusCode.OK); }
        ]);
        using var provider = BuildProvider(scenario);
        var helper = Helper(provider);
        using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(100));

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            helper.SendAsync($"http://{Host}/slow", HttpMethod.Get, cancellationToken: cts.Token));
    }

    [Fact]
    public async Task SlowLegacyMock_WithAddTimeout_Returns408() {
        output.WriteLine("[Scenario] Mock legacy (factory senza token) il cui Task non completa mai; helper con addTimeout di 100 ms");
        output.WriteLine("[Atteso] 408 Request Timeout entro 5 s anche se il mock ignora la cancellazione");

        var never = new TaskCompletionSource<HttpResponseMessage>(TaskCreationOptions.RunContinuationsAsynchronously);
        try {
            var scenario = new HttpMockScenario(r => r.RequestUri?.Host == Host, [() => never.Task]);
            using var provider = BuildProvider(scenario);
            var helper = Helper(provider).addTimeout(TimeSpan.FromMilliseconds(100));

            var started = DateTime.UtcNow;
            using var response = await helper.SendAsync($"http://{Host}/slow", HttpMethod.Get);
            Got(response);

            Assert.Equal(HttpStatusCode.RequestTimeout, response.StatusCode);
            Assert.True(DateTime.UtcNow - started < TimeSpan.FromSeconds(5));
        } finally {
            using var leftover = new HttpResponseMessage(HttpStatusCode.OK);
            never.TrySetResult(leftover);
        }
    }

    private sealed class ThrowingHandler : HttpMessageHandler {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
            throw new Xunit.Sdk.XunitException("real network reached");
    }
}
