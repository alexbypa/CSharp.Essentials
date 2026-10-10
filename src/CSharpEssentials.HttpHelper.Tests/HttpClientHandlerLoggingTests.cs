using System.Diagnostics;
using System.Net;
using CSharpEssentials.HttpHelper.HttpMocks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Serilog;
using Serilog.Core;
using Serilog.Events;
using Xunit.Abstractions;

namespace CSharpEssentials.HttpHelper.Tests;

/// <summary>Opt-in per-attempt request log (HttpHelperLogging:LogRequests), exercised through the public AddHttpClients/factory path.</summary>
[Collection(HttpHelperLogCollection.Name)]
public class HttpClientHandlerLoggingTests(ITestOutputHelper output) {
    private const string Host = "mock.local";

    private sealed class CaptureSink : ILogEventSink {
        public List<LogEvent> Events { get; } = [];
        public void Emit(LogEvent logEvent) => Events.Add(logEvent);
    }

    private sealed class Env : IDisposable {
        public CaptureSink Capture { get; } = new();
        public ServiceProvider Provider { get; }
        public IConfigurationRoot Config { get; }
        private readonly ILogger _previous = Log.Logger;
        public Env(Dictionary<string, string?> settings, LogEventLevel minLevel, params IHttpMockScenario[] scenarios) {
            Log.Logger = new LoggerConfiguration().MinimumLevel.Is(minLevel).WriteTo.Sink(Capture).CreateLogger();
            settings["HttpClientOptions:0:Name"] = "A";
            Config = new ConfigurationBuilder().AddInMemoryCollection(settings).Build();
            var services = new ServiceCollection();
            foreach (var s in scenarios)
                services.AddSingleton(s);
            services.AddHttpClients(Config);
            Provider = services.BuildServiceProvider();
        }
        public IhttpsClientHelper Helper => Provider.GetRequiredService<IhttpsClientHelperFactory>().CreateOrGet("A");
        public List<LogEvent> RequestEvents => Capture.Events.Where(e => e.Properties.ContainsKey("HttpClientName")).ToList();
        public void Dispose() {
            Provider.Dispose();
            Log.Logger = _previous;
        }
    }

    private static Dictionary<string, string?> Logging(params string[] clients) {
        var d = new Dictionary<string, string?>();
        for (int i = 0; i < clients.Length; i++)
            d[$"HttpHelperLogging:LogRequests:{i}"] = clients[i];
        return d;
    }

    private static HttpMockScenario Scenario(string path, params Func<Task<HttpResponseMessage>>[] responses) =>
        new(r => r.RequestUri?.Host == Host && r.RequestUri.AbsolutePath == path, responses.ToList());

    private static Func<Task<HttpResponseMessage>> Status(HttpStatusCode code) => () => Task.FromResult(new HttpResponseMessage(code));

    private static string Prop(LogEvent e, string name) => e.Properties[name].ToString().Trim('"');

    private void Got(Env env) {
        var events = env.RequestEvents;
        output.WriteLine($"[Restituito] {events.Count} evento/i: " + string.Join(" | ", events.Select(e => $"{e.Level} {e.RenderMessage()}")));
    }

    [Fact]
    public async Task SendAsync_RetryAfter500_LogsOneEventPerAttempt() {
        output.WriteLine("[Scenario] Client A in LogRequests; mock /e risponde 500 e poi 200; retry su 5xx (1 retry, backoff 0)");
        output.WriteLine("[Atteso] 2 eventi: RetryAttempt 0 livello Error (500), RetryAttempt 1 livello Information (200), HttpClientName=A");

        using var env = new Env(Logging("A"), LogEventLevel.Verbose, Scenario("/e", Status(HttpStatusCode.InternalServerError), Status(HttpStatusCode.OK)));
        var helper = env.Helper.addRetryCondition(r => (int)r.StatusCode >= 500, 1, 0);

        using var response = await helper.SendAsync($"http://{Host}/e", HttpMethod.Get);
        Got(env);

        var events = env.RequestEvents;
        Assert.Equal(2, events.Count);
        Assert.Equal(LogEventLevel.Error, events[0].Level);
        Assert.Equal("0", Prop(events[0], "RetryAttempt"));
        Assert.Equal("500", Prop(events[0], "StatusCode"));
        Assert.Equal(LogEventLevel.Information, events[1].Level);
        Assert.Equal("1", Prop(events[1], "RetryAttempt"));
        Assert.All(events, e => {
            Assert.Equal("A", Prop(e, "HttpClientName"));
            Assert.Equal("GET", Prop(e, "Method"));
            Assert.Equal("/e", Prop(e, "RequestPath"));
            Assert.True(e.Properties.ContainsKey("ElapsedMs"));
        });
    }

    [Theory]
    [InlineData(HttpStatusCode.NotFound, LogEventLevel.Warning)]
    [InlineData(HttpStatusCode.RequestTimeout, LogEventLevel.Error)]
    public async Task SendAsync_ClientErrorStatus_LogsExpectedLevel(HttpStatusCode code, LogEventLevel expected) {
        output.WriteLine($"[Scenario] Client A in LogRequests; mock /s risponde {(int)code}");
        output.WriteLine($"[Atteso] Un evento di livello {expected}");

        using var env = new Env(Logging("A"), LogEventLevel.Verbose, Scenario("/s", Status(code)));

        using var response = await env.Helper.SendAsync($"http://{Host}/s", HttpMethod.Get);
        Got(env);

        var evt = Assert.Single(env.RequestEvents);
        Assert.Equal(expected, evt.Level);
        Assert.Equal(((int)code).ToString(), Prop(evt, "StatusCode"));
    }

    [Fact]
    public async Task SendAsync_UrlWithQuery_LogsPathOnly() {
        output.WriteLine("[Scenario] Client A in LogRequests; GET /q?token=SECRET con header Authorization");
        output.WriteLine("[Atteso] RequestPath=/q; nessuna proprieta e nessun messaggio contiene la query o gli header");

        using var env = new Env(Logging("A"), LogEventLevel.Verbose, Scenario("/q", Status(HttpStatusCode.OK)));
        using var response = await env.Helper.SendAsync($"http://{Host}/q?token=SECRET", HttpMethod.Get,
            headers: new Dictionary<string, string> { ["Authorization"] = "Bearer TOPSECRET" });
        Got(env);

        var evt = Assert.Single(env.RequestEvents);
        Assert.Equal("/q", Prop(evt, "RequestPath"));
        var all = evt.RenderMessage() + string.Join(";", evt.Properties.Values.Select(v => v.ToString()));
        Assert.DoesNotContain("SECRET", all);
        Assert.DoesNotContain("token", all);
    }

    [Theory]
    [InlineData("Other")]
    [InlineData(null)]
    public async Task SendAsync_ClientNotListedOrSectionMissing_LogsNothing(string? listed) {
        output.WriteLine($"[Scenario] LogRequests = {listed ?? "sezione assente"}; richiesta dal client A");
        output.WriteLine("[Atteso] Nessun evento di richiesta");

        using var env = new Env(listed is null ? new Dictionary<string, string?>() : Logging(listed), LogEventLevel.Verbose, Scenario("/n", Status(HttpStatusCode.OK)));
        using var response = await env.Helper.SendAsync($"http://{Host}/n", HttpMethod.Get);
        Got(env);

        Assert.Empty(env.RequestEvents);
    }

    [Fact]
    public async Task SendAsync_WildcardConfigured_LogsAnyClient() {
        output.WriteLine("[Scenario] LogRequests = [\"*\"]; richiesta dal client A");
        output.WriteLine("[Atteso] Un evento con HttpClientName=A");

        using var env = new Env(Logging("*"), LogEventLevel.Verbose, Scenario("/w", Status(HttpStatusCode.OK)));
        using var response = await env.Helper.SendAsync($"http://{Host}/w", HttpMethod.Get);
        Got(env);

        Assert.Equal("A", Prop(Assert.Single(env.RequestEvents), "HttpClientName"));
    }

    [Fact]
    public async Task SendAsync_OptionsChangedAtRuntime_HonouredOnNextRequest() {
        output.WriteLine("[Scenario] Config iniziale senza log; due richieste, in mezzo reload della config con LogRequests=[A]");
        output.WriteLine("[Atteso] Nessun evento dopo la prima richiesta, uno dopo la seconda");

        using var env = new Env([], LogEventLevel.Verbose, Scenario("/r", Status(HttpStatusCode.OK), Status(HttpStatusCode.OK)));
        var helper = env.Helper;

        using (await helper.SendAsync($"http://{Host}/r", HttpMethod.Get)) { }
        Assert.Empty(env.RequestEvents);

        env.Config["HttpHelperLogging:LogRequests:0"] = "A";
        env.Config.Reload();
        using (await helper.SendAsync($"http://{Host}/r", HttpMethod.Get)) { }
        Got(env);

        Assert.Single(env.RequestEvents);
    }

    [Fact]
    public async Task SendAsync_LevelDisabledInLogger_LogsNothing() {
        output.WriteLine("[Scenario] Client A in LogRequests ma livello minimo del logger = Fatal; mock risponde 200");
        output.WriteLine("[Atteso] Nessun evento emesso e risposta 200");

        using var env = new Env(Logging("A"), LogEventLevel.Fatal, Scenario("/l", Status(HttpStatusCode.OK)));
        using var response = await env.Helper.SendAsync($"http://{Host}/l", HttpMethod.Get);
        Got(env);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Empty(env.RequestEvents);
    }

    [Fact]
    public async Task SendAsync_TransportException_LogsErrorWithExceptionAndHelperReturns502() {
        output.WriteLine("[Scenario] Client A in LogRequests; il mock lancia HttpRequestException");
        output.WriteLine("[Atteso] Evento Error 'failed' con eccezione; l'helper restituisce comunque 502");

        using var env = new Env(Logging("A"), LogEventLevel.Verbose, Scenario("/x", () => throw new HttpRequestException("boom")));
        using var response = await env.Helper.SendAsync($"http://{Host}/x", HttpMethod.Get);
        Got(env);

        Assert.Equal(HttpStatusCode.BadGateway, response.StatusCode);
        var evt = Assert.Single(env.RequestEvents);
        Assert.Equal(LogEventLevel.Error, evt.Level);
        Assert.IsType<HttpRequestException>(evt.Exception);
        Assert.Contains("failed", evt.RenderMessage());
        Assert.False(evt.Properties.ContainsKey("StatusCode"));
    }

    private const string CidHeader = "X-Correlation-ID";

    private static Dictionary<string, string?> Cid(Dictionary<string, string?> d, string? header = CidHeader) {
        d["HttpHelperLogging:CorrelationIdHeader"] = header;
        return d;
    }

    /// <summary>Scenario whose factories record the value of the correlation header seen on each attempt.</summary>
    private static HttpMockScenario Recording(string path, List<string?> seen, params HttpStatusCode[] codes) =>
        new(r => r.RequestUri?.Host == Host && r.RequestUri.AbsolutePath == path,
            codes.Select<HttpStatusCode, Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>>>(c => (r, _) => {
                seen.Add(r.Headers.TryGetValues(CidHeader, out var v) ? string.Join(",", v) : null);
                return Task.FromResult(new HttpResponseMessage(c));
            }).ToList());

    [Fact]
    public async Task SendAsync_CorrelationHeaderConfigured_AddsGeneratedId() {
        output.WriteLine("[Scenario] CorrelationIdHeader=X-Correlation-ID, nessuna Activity; GET senza header");
        output.WriteLine("[Atteso] Il mock vede X-Correlation-ID con 32 caratteri esadecimali");

        var seen = new List<string?>();
        using var env = new Env(Cid(Logging("A")), LogEventLevel.Verbose, Recording("/c1", seen, HttpStatusCode.OK));
        using var response = await env.Helper.SendAsync($"http://{Host}/c1", HttpMethod.Get);
        output.WriteLine($"[Restituito] header visto: {string.Join(",", seen)}");

        var id = Assert.Single(seen);
        Assert.Matches("^[0-9a-f]{32}$", id);
    }

    [Fact]
    public async Task SendAsync_RetriesWithCorrelation_SameIdOnEveryAttemptAndInEvents() {
        output.WriteLine("[Scenario] Correlation + LogRequests; mock 500, 500, 200 con 2 retry");
        output.WriteLine("[Atteso] Stesso id nei 3 tentativi e nella proprieta CorrelationId dei 3 eventi");

        var seen = new List<string?>();
        using var env = new Env(Cid(Logging("A")), LogEventLevel.Verbose,
            Recording("/c2", seen, HttpStatusCode.InternalServerError, HttpStatusCode.InternalServerError, HttpStatusCode.OK));
        var helper = env.Helper.addRetryCondition(r => (int)r.StatusCode >= 500, 2, 0);
        using var response = await helper.SendAsync($"http://{Host}/c2", HttpMethod.Get);
        Got(env);
        output.WriteLine($"[Restituito] header visti: {string.Join(" | ", seen)}");

        Assert.Equal(3, seen.Count);
        Assert.Single(seen.Distinct());
        Assert.NotNull(seen[0]);
        var events = env.RequestEvents;
        Assert.Equal(3, events.Count);
        Assert.All(events, e => Assert.Equal(seen[0], Prop(e, "CorrelationId")));
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task SendAsync_CallerSetsHeader_ValueKept(bool perRequest) {
        output.WriteLine($"[Scenario] Correlation attiva; header del chiamante ({(perRequest ? "per-request headers" : "addHeaders/default")}) = mine-123; 1 retry");
        output.WriteLine("[Atteso] Il mock vede sempre mine-123 e gli eventi riportano CorrelationId=mine-123");

        var seen = new List<string?>();
        using var env = new Env(Cid(Logging("A")), LogEventLevel.Verbose, Recording("/c3", seen, HttpStatusCode.InternalServerError, HttpStatusCode.OK));
        var helper = env.Helper.addRetryCondition(r => (int)r.StatusCode >= 500, 1, 0);
        if (!perRequest)
            helper.addHeaders(CidHeader, "mine-123");
        using var response = await helper.SendAsync($"http://{Host}/c3", HttpMethod.Get,
            headers: perRequest ? new Dictionary<string, string> { [CidHeader] = "mine-123" } : null);
        Got(env);
        output.WriteLine($"[Restituito] header visti: {string.Join(" | ", seen)}");

        Assert.Equal(["mine-123", "mine-123"], seen);
        Assert.All(env.RequestEvents, e => Assert.Equal("mine-123", Prop(e, "CorrelationId")));
    }

    [Fact]
    public async Task SendAsync_ActiveActivity_UsesItsTraceId() {
        output.WriteLine("[Scenario] Correlation attiva; richiesta dentro un'Activity W3C attiva");
        output.WriteLine("[Atteso] Il header vale il TraceId dell'Activity");

        using var source = new ActivitySource("HttpHelper.Tests.Cid");
        using var listener = new ActivityListener {
            ShouldListenTo = s => s.Name == source.Name,
            Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllData
        };
        ActivitySource.AddActivityListener(listener);
        var seen = new List<string?>();
        using var env = new Env(Cid(Logging()), LogEventLevel.Verbose, Recording("/c4", seen, HttpStatusCode.OK));

        string traceId;
        using (var activity = source.StartActivity("op")) {
            Assert.NotNull(activity);
            traceId = activity.TraceId.ToHexString();
            using var response = await env.Helper.SendAsync($"http://{Host}/c4", HttpMethod.Get);
        }
        output.WriteLine($"[Restituito] header visto: {string.Join(",", seen)}; TraceId {traceId}");

        Assert.Equal(traceId, Assert.Single(seen));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public async Task SendAsync_CorrelationOffOrMissing_AddsNoHeader(string? header) {
        output.WriteLine($"[Scenario] CorrelationIdHeader = {(header is null ? "assente" : "vuoto")}");
        output.WriteLine("[Atteso] Nessun header di correlazione visto dal mock");

        var settings = Logging("A");
        if (header is not null)
            Cid(settings, header);
        var seen = new List<string?>();
        using var env = new Env(settings, LogEventLevel.Verbose, Recording("/c5", seen, HttpStatusCode.OK));
        using var response = await env.Helper.SendAsync($"http://{Host}/c5", HttpMethod.Get);
        Got(env);

        Assert.Null(Assert.Single(seen));
        Assert.All(env.RequestEvents, e => Assert.False(e.Properties.ContainsKey("CorrelationId")));
    }

    [Fact]
    public async Task SendAsync_CorrelationHeaderNameInvalid_NoHeaderAndNoCorrelationIdProperty() {
        output.WriteLine("[Scenario] CorrelationIdHeader = \"Bad Name\" (token non valido), LogRequests = A");
        output.WriteLine("[Atteso] Nessun header inviato e nessuna proprietà CorrelationId negli eventi");

        var settings = Logging("A");
        Cid(settings, "Bad Name");
        var seen = new List<string?>();
        using var env = new Env(settings, LogEventLevel.Verbose, Recording("/c5b", seen, HttpStatusCode.OK));
        using var response = await env.Helper.SendAsync($"http://{Host}/c5b", HttpMethod.Get);
        Got(env);

        Assert.Null(Assert.Single(seen));
        Assert.All(env.RequestEvents, e => Assert.False(e.Properties.ContainsKey("CorrelationId")));
    }

    [Fact]
    public async Task SendAsync_CorrelationOnLogRequestsOff_AddsHeaderWithoutEvents() {
        output.WriteLine("[Scenario] Correlation attiva, LogRequests vuoto");
        output.WriteLine("[Atteso] Header presente, nessun evento di richiesta");

        var seen = new List<string?>();
        using var env = new Env(Cid([]), LogEventLevel.Verbose, Recording("/c6", seen, HttpStatusCode.OK));
        using var response = await env.Helper.SendAsync($"http://{Host}/c6", HttpMethod.Get);
        Got(env);

        Assert.Matches("^[0-9a-f]{32}$", Assert.Single(seen));
        Assert.Empty(env.RequestEvents);
    }

    [Fact]
    public async Task SendAsync_CallerCancelsInFlight_LogsOneWarningAndPropagates() {
        output.WriteLine("[Scenario] Client A in LogRequests; il token del chiamante viene annullato mentre il mock e in volo");
        output.WriteLine("[Atteso] L'helper propaga OperationCanceledException; un solo evento Warning 'failed' con l'eccezione");

        using var cts = new CancellationTokenSource();
        var scenario = new HttpMockScenario(r => r.RequestUri?.Host == Host && r.RequestUri.AbsolutePath == "/cx",
            [async (_, ct) => {
                await cts.CancelAsync();
                ct.ThrowIfCancellationRequested();
                return new HttpResponseMessage(HttpStatusCode.OK);
            }]);
        using var env = new Env(Logging("A"), LogEventLevel.Verbose, scenario);

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            env.Helper.SendAsync($"http://{Host}/cx", HttpMethod.Get, cancellationToken: cts.Token));
        Got(env);

        var evt = Assert.Single(env.RequestEvents);
        Assert.Equal(LogEventLevel.Warning, evt.Level);
        Assert.IsAssignableFrom<OperationCanceledException>(evt.Exception);
        Assert.Contains("failed", evt.RenderMessage());
    }

    [Fact]
    public async Task SendAsync_HelperTimeout_LogsOneErrorAndReturns408() {
        output.WriteLine("[Scenario] Client A in LogRequests; il mock non risponde mai e l'helper ha addTimeout(100 ms)");
        output.WriteLine("[Atteso] Risposta 408; un solo evento 'failed' di livello Error (timeout, non cancel del chiamante)");

        var scenario = new HttpMockScenario(r => r.RequestUri?.Host == Host && r.RequestUri.AbsolutePath == "/to",
            [async (_, ct) => {
                await Task.Delay(Timeout.Infinite, ct);
                return new HttpResponseMessage(HttpStatusCode.OK);
            }]);
        using var env = new Env(Logging("A"), LogEventLevel.Verbose, scenario);
        env.Helper.addTimeout(TimeSpan.FromMilliseconds(100));

        using var response = await env.Helper.SendAsync($"http://{Host}/to", HttpMethod.Get);
        Got(env);

        Assert.Equal(HttpStatusCode.RequestTimeout, response.StatusCode);
        var evt = Assert.Single(env.RequestEvents);
        Assert.Equal(LogEventLevel.Error, evt.Level);
        Assert.IsAssignableFrom<OperationCanceledException>(evt.Exception);
        Assert.Contains("failed", evt.RenderMessage());
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task SendAsync_TimeoutThen503Then200WithCallerCorrelation_LogsOneEventPerAttemptOrNoneWhenOff(bool logOn) {
        output.WriteLine($"[Scenario] Flusso Demo 'order' (LogRequests {(logOn ? "A" : "vuoto")}, Correlation attiva): timeout, 503, 200; addTimeout(100 ms), retry su 5xx||408 (3, backoff 0); header per-request X-Correlation-ID=order-42");
        output.WriteLine(logOn
            ? "[Atteso] 200 finale; header order-42 sui 3 tentativi; 3 eventi: Error (OCE 'failed'), Error, Information; RetryAttempt 0/1/2; CorrelationId order-42"
            : "[Atteso] 200 finale; header order-42 sui 3 tentativi; nessun evento di richiesta");

        var seen = new List<string?>();
        Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> Record(HttpStatusCode? code) => async (r, ct) => {
            seen.Add(r.Headers.TryGetValues(CidHeader, out var v) ? string.Join(",", v) : null);
            if (code is null)
                await Task.Delay(Timeout.Infinite, ct);
            return new HttpResponseMessage(code ?? HttpStatusCode.OK);
        };
        var scenario = new HttpMockScenario(r => r.RequestUri?.Host == Host && r.RequestUri.AbsolutePath == "/order",
            [Record(null), Record(HttpStatusCode.ServiceUnavailable), Record(HttpStatusCode.OK)]);
        using var env = new Env(Cid(logOn ? Logging("A") : []), LogEventLevel.Verbose, scenario);
        var helper = env.Helper.addTimeout(TimeSpan.FromMilliseconds(100))
            .addRetryCondition(r => (int)r.StatusCode >= 500 || r.StatusCode == HttpStatusCode.RequestTimeout, 3, 0);

        using var response = await helper.SendAsync($"http://{Host}/order", HttpMethod.Get,
            headers: new Dictionary<string, string> { [CidHeader] = "order-42" });
        Got(env);
        output.WriteLine($"[Restituito] header visti: {string.Join(" | ", seen)}; stato finale {(int)response.StatusCode}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(["order-42", "order-42", "order-42"], seen);
        var events = env.RequestEvents;
        if (!logOn) {
            Assert.Empty(events);
            return;
        }
        Assert.Equal([LogEventLevel.Error, LogEventLevel.Error, LogEventLevel.Information], events.Select(e => e.Level));
        Assert.IsAssignableFrom<OperationCanceledException>(events[0].Exception);
        Assert.Contains("failed", events[0].RenderMessage());
        Assert.Equal(["0", "1", "2"], events.Select(e => Prop(e, "RetryAttempt")));
        Assert.All(events, e => Assert.Equal("order-42", Prop(e, "CorrelationId")));
    }

    // ---- 4-param public ctor with injected Serilog logger (A87) ----

    private sealed class StubInner(params HttpStatusCode[] codes) : HttpMessageHandler {
        private readonly Queue<HttpStatusCode> _codes = new(codes);
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
            Task.FromResult(new HttpResponseMessage(_codes.Dequeue()));
    }

    private static IOptionsMonitor<HttpHelperLoggingOptions> Monitor(string[] logRequests, string? cidHeader = null) =>
        new ServiceCollection()
            .Configure<HttpHelperLoggingOptions>(o => { o.LogRequests = [.. logRequests]; o.CorrelationIdHeader = cidHeader; })
            .BuildServiceProvider()
            .GetRequiredService<IOptionsMonitor<HttpHelperLoggingOptions>>();

    private static HttpClientHandlerLogging Handler(IOptionsMonitor<HttpHelperLoggingOptions> opts, Serilog.ILogger? logger, params HttpStatusCode[] codes) =>
        new(new HttpRequestEvents(), null, "X", opts, logger) { InnerHandler = new StubInner(codes) };

    private static Serilog.ILogger CaptureLogger(CaptureSink sink) => new LoggerConfiguration().MinimumLevel.Verbose().WriteTo.Sink(sink).CreateLogger();

    private static async Task Send(HttpClientHandlerLogging handler, int attempt, string? cid = null) {
        using var invoker = new HttpMessageInvoker(handler, disposeHandler: false);
        using var req = new HttpRequestMessage(HttpMethod.Get, $"http://{Host}/inj");
        req.Headers.Add("X-Retry-Attempt", attempt.ToString());
        if (cid is not null)
            req.Headers.Add(CidHeader, cid);
        using var _ = await invoker.SendAsync(req, CancellationToken.None);
    }

    [Fact]
    public async Task SendAsync_InjectedLogger_ReceivesOneEventPerAttemptAndNotLogLogger() {
        output.WriteLine("[Scenario] Ctor pubblico con logger iniettato; client X in LogRequests, CorrelationIdHeader impostato; inner 500 poi 200 (X-Retry-Attempt 0 e 1, stesso id)");
        output.WriteLine("[Atteso] 2 eventi nel sink iniettato (RetryAttempt 0/1, stesso CorrelationId, HttpClientName=X), 0 in Log.Logger");

        var injected = new CaptureSink();
        var global = new CaptureSink();
        var previous = Log.Logger;
        Log.Logger = CaptureLogger(global);
        try {
            var handler = Handler(Monitor(["X"], CidHeader), CaptureLogger(injected), HttpStatusCode.InternalServerError, HttpStatusCode.OK);
            await Send(handler, 0, "cid-1");
            await Send(handler, 1, "cid-1");
        } finally {
            Log.Logger = previous;
        }
        output.WriteLine($"[Restituito] iniettato={injected.Events.Count} globale={global.Events.Count}");

        Assert.Equal(2, injected.Events.Count);
        Assert.Empty(global.Events);
        Assert.Equal(["0", "1"], injected.Events.Select(e => Prop(e, "RetryAttempt")));
        Assert.All(injected.Events, e => {
            Assert.Equal("X", Prop(e, "HttpClientName"));
            Assert.Equal("cid-1", Prop(e, "CorrelationId"));
        });
    }

    private sealed class CancelingInner : HttpMessageHandler {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
            throw new TaskCanceledException();
    }

    [Fact]
    public async Task SendAsync_CancelWithoutHelperMarker_LogsWarning() {
        output.WriteLine("[Scenario] Handler costruito a mano (senza httpsClientHelper, quindi senza CallerTokenKey); l'inner lancia TaskCanceledException");
        output.WriteLine("[Atteso] Un solo evento 'failed' di livello Warning (timeout e cancel non distinguibili) e l'eccezione si propaga");

        var injected = new CaptureSink();
        using var handler = new HttpClientHandlerLogging(new HttpRequestEvents(), null, "X", Monitor(["X"]), CaptureLogger(injected)) { InnerHandler = new CancelingInner() };
        using var invoker = new HttpMessageInvoker(handler, disposeHandler: false);
        using var req = new HttpRequestMessage(HttpMethod.Get, $"http://{Host}/nm");

        await Assert.ThrowsAsync<TaskCanceledException>(() => invoker.SendAsync(req, CancellationToken.None));
        output.WriteLine($"[Restituito] {injected.Events.Count} eventi");

        var evt = Assert.Single(injected.Events);
        Assert.Equal(LogEventLevel.Warning, evt.Level);
    }

    [Fact]
    public async Task SendAsync_InjectedLoggerClientNotListed_LogsNothing() {
        output.WriteLine("[Scenario] Logger iniettato ma LogRequests = [Other]; client X");
        output.WriteLine("[Atteso] Nessun evento nel sink iniettato");

        var injected = new CaptureSink();
        await Send(Handler(Monitor(["Other"]), CaptureLogger(injected), HttpStatusCode.OK), 0);
        output.WriteLine($"[Restituito] {injected.Events.Count} eventi");

        Assert.Empty(injected.Events);
    }

    [Fact]
    public async Task SendAsync_NullLogger_FallsBackToLogLogger() {
        output.WriteLine("[Scenario] Ctor pubblico con logger null; client X in LogRequests; Log.Logger = capture");
        output.WriteLine("[Atteso] Un evento con HttpClientName=X in Log.Logger (fallback)");

        var global = new CaptureSink();
        var previous = Log.Logger;
        Log.Logger = CaptureLogger(global);
        try {
            await Send(Handler(Monitor(["X"]), null, HttpStatusCode.OK), 0);
        } finally {
            Log.Logger = previous;
        }
        output.WriteLine($"[Restituito] {global.Events.Count} eventi");

        Assert.Equal("X", Prop(Assert.Single(global.Events, e => e.Properties.ContainsKey("HttpClientName")), "HttpClientName"));
    }

    [Fact]
    public void Ctor_NullLoggingOptions_ThrowsArgumentNullException() {
        output.WriteLine("[Scenario] Ctor pubblico con IOptionsMonitor null");
        output.WriteLine("[Atteso] ArgumentNullException (logging)");

        var ex = Assert.Throws<ArgumentNullException>(() => new HttpClientHandlerLogging(new HttpRequestEvents(), null, "X", null!));
        Assert.Equal("logging", ex.ParamName);
    }

    [Fact]
    public void Ctor_NullClientName_ThrowsArgumentNullException() {
        output.WriteLine("[Scenario] Ctor pubblico con clientName null");
        output.WriteLine("[Atteso] ArgumentNullException (clientName)");

        var ex = Assert.Throws<ArgumentNullException>(() => new HttpClientHandlerLogging(new HttpRequestEvents(), null, null!, Monitor(["X"])));
        Assert.Equal("clientName", ex.ParamName);
    }
}
