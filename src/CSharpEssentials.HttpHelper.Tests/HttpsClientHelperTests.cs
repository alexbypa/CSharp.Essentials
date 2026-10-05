using Xunit.Abstractions;
using System.Net;

namespace CSharpEssentials.HttpHelper.Tests;

public class HttpsClientHelperTests {
    private readonly ITestOutputHelper _output;

    public HttpsClientHelperTests(ITestOutputHelper output) {
        _output = output;
    }

    private const string Url = "http://stub.local/api";

    [Fact]
    public async Task SendAsync_NoRetry_ReturnsHandlerResponse() {
        _output.WriteLine("[Scenario] Helper senza retry; lo stub risponde 202 Accepted a GET http://stub.local/api");
        _output.WriteLine("[Atteso] Viene restituita la risposta dello stub (202) con una sola chiamata all'handler");

        var stub = new StubHandler(_ => new HttpResponseMessage(HttpStatusCode.Accepted));
        using var client = new HttpClient(stub);
        var helper = new httpsClientHelper(client, new HttpRequestEvents(), null!);

        using var response = await helper.SendAsync(Url, HttpMethod.Get);

        _output.WriteLine($"[Restituito] Status={(int)response.StatusCode}, ChiamateHandler={stub.Calls}");
        Assert.Equal(HttpStatusCode.Accepted, response.StatusCode);
        Assert.Equal(1, stub.Calls);
    }

    [Fact]
    public async Task SendAsync_WithHeaders_AddsPerRequestHeaders() {
        _output.WriteLine("[Scenario] GET con il dizionario headers { \"X-Test\": \"value\" } passato a SendAsync");
        _output.WriteLine("[Atteso] La richiesta arrivata all'handler contiene un solo header X-Test con valore \"value\"");

        var stub = new StubHandler(_ => new HttpResponseMessage(HttpStatusCode.OK));
        using var client = new HttpClient(stub);
        var helper = new httpsClientHelper(client, new HttpRequestEvents(), null!);

        using var response = await helper.SendAsync(Url, HttpMethod.Get,
            headers: new Dictionary<string, string> { ["X-Test"] = "value" });

        _output.WriteLine($"[Restituito] X-Test=[{string.Join(", ", stub.LastRequest!.Headers.GetValues("X-Test"))}]");
        Assert.Equal("value", Assert.Single(stub.LastRequest!.Headers.GetValues("X-Test")));
    }

    [Fact]
    public async Task SendAsync_JsonContentBuilder_SendsJsonBody() {
        _output.WriteLine("[Scenario] POST con JsonContentBuilder e payload {\"id\":1}");
        _output.WriteLine("[Atteso] L'handler riceve il body JSON invariato e Content-Type \"application/json\"");

        const string json = "{\"id\":1}";
        string? body = null;
        string? mediaType = null;
        var stub = new StubHandler(r => {
            body = r.Content!.ReadAsStringAsync().Result;
            mediaType = r.Content.Headers.ContentType?.MediaType;
            return new HttpResponseMessage(HttpStatusCode.OK);
        });
        using var client = new HttpClient(stub);
        var helper = new httpsClientHelper(client, new HttpRequestEvents(), null!);

        using var response = await helper.SendAsync(Url, HttpMethod.Post, new JsonContentBuilder(), json);

        _output.WriteLine($"[Restituito] Body={body}, MediaType={mediaType}");
        Assert.Equal(json, body);
        Assert.Equal("application/json", mediaType);
    }

    [Fact]
    public async Task SendAsync_RetryConditionMet_RetriesUntilSuccess() {
        _output.WriteLine("[Scenario] Retry su !IsSuccessStatusCode (3 retry, backoff 0); lo stub risponde 500, 500 e poi 200");
        _output.WriteLine("[Atteso] 200 OK alla terza chiamata (3 chiamate totali) e ultimo header X-Retry-Attempt = \"2\"");

        var statuses = new Queue<HttpStatusCode>([HttpStatusCode.InternalServerError, HttpStatusCode.InternalServerError, HttpStatusCode.OK]);
        var stub = new StubHandler(_ => new HttpResponseMessage(statuses.Dequeue()));
        using var client = new HttpClient(stub);
        var helper = new httpsClientHelper(client, new HttpRequestEvents(), null!)
            .addRetryCondition(r => !r.IsSuccessStatusCode, 3, 0);

        using var response = await helper.SendAsync(Url, HttpMethod.Get);

        _output.WriteLine($"[Restituito] Status={(int)response.StatusCode}, ChiamateHandler={stub.Calls}, X-Retry-Attempt=[{string.Join(", ", stub.LastRequest!.Headers.GetValues("X-Retry-Attempt"))}]");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(3, stub.Calls);
        Assert.Equal("2", Assert.Single(stub.LastRequest!.Headers.GetValues("X-Retry-Attempt")));
    }

    [Fact]
    public async Task SendAsync_RetryWithJsonBody_ResendsBodyAndContentTypeOnEachAttempt() {
        _output.WriteLine("[Scenario] POST JSON {\"id\":1} con retry (3 retry); lo stub risponde 500, 500 e poi 200");
        _output.WriteLine("[Atteso] 200 OK e il body JSON con Content-Type application/json viene rinviato identico a ogni tentativo (3 volte)");

        const string json = "{\"id\":1}";
        var statuses = new Queue<HttpStatusCode>([HttpStatusCode.InternalServerError, HttpStatusCode.InternalServerError, HttpStatusCode.OK]);
        var bodies = new List<string>();
        var mediaTypes = new List<string?>();
        var stub = new StubHandler(r => {
            bodies.Add(r.Content!.ReadAsStringAsync().Result);
            mediaTypes.Add(r.Content.Headers.ContentType?.MediaType);
            return new HttpResponseMessage(statuses.Dequeue());
        });
        using var client = new HttpClient(stub);
        var helper = new httpsClientHelper(client, new HttpRequestEvents(), null!)
            .addRetryCondition(r => !r.IsSuccessStatusCode, 3, 0);

        using var response = await helper.SendAsync(Url, HttpMethod.Post, new JsonContentBuilder(), json);

        _output.WriteLine($"[Restituito] Status={(int)response.StatusCode}, Body inviati=[{string.Join(" | ", bodies)}], MediaType=[{string.Join(", ", mediaTypes)}]");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal([json, json, json], bodies);
        Assert.All(mediaTypes, m => Assert.Equal("application/json", m));
    }

    [Fact]
    public async Task SendAsync_RetryExhausted_ReturnsLastFailure() {
        _output.WriteLine("[Scenario] Lo stub risponde sempre 500; retry su !IsSuccessStatusCode con 2 retry");
        _output.WriteLine("[Atteso] Retry esauriti: viene restituito l'ultimo 500 dopo 3 chiamate totali (1 + 2 retry)");

        var stub = new StubHandler(_ => new HttpResponseMessage(HttpStatusCode.InternalServerError));
        using var client = new HttpClient(stub);
        var helper = new httpsClientHelper(client, new HttpRequestEvents(), null!)
            .addRetryCondition(r => !r.IsSuccessStatusCode, 2, 0);

        using var response = await helper.SendAsync(Url, HttpMethod.Get);

        _output.WriteLine($"[Restituito] Status={(int)response.StatusCode}, ChiamateHandler={stub.Calls}");
        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
        Assert.Equal(3, stub.Calls);
    }

    [Fact]
    public async Task SendAsync_HandlerThrowsHttpRequestException_Returns502() {
        _output.WriteLine("[Scenario] L'handler lancia HttpRequestException(\"boom\")");
        _output.WriteLine("[Atteso] L'eccezione non esce dall'helper: viene restituito 502 Bad Gateway");

        var stub = new StubHandler(_ => throw new HttpRequestException("boom"));
        using var client = new HttpClient(stub);
        var helper = new httpsClientHelper(client, new HttpRequestEvents(), null!);

        using var response = await helper.SendAsync(Url, HttpMethod.Get);

        _output.WriteLine($"[Restituito] Status={(int)response.StatusCode}");
        Assert.Equal(HttpStatusCode.BadGateway, response.StatusCode);
    }

    [Fact]
    public async Task SendAsync_EmptyUrl_ThrowsInvalidOperationException() {
        _output.WriteLine("[Scenario] SendAsync con URL stringa vuota");
        _output.WriteLine("[Atteso] InvalidOperationException e nessuna chiamata all'handler (Calls == 0)");

        var stub = new StubHandler(_ => new HttpResponseMessage(HttpStatusCode.OK));
        using var client = new HttpClient(stub);
        var helper = new httpsClientHelper(client, new HttpRequestEvents(), null!);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => helper.SendAsync("", HttpMethod.Get));
        _output.WriteLine($"[Restituito] {ex.GetType().Name}, ChiamateHandler={stub.Calls}");

        Assert.Equal(0, stub.Calls);
    }

    [Fact]
    public async Task SendAsync_EmptyUrlWithRetryPolicy_ThrowsInvalidOperationException() {
        _output.WriteLine("[Scenario] SendAsync con URL null su un helper con politica di retry attiva (2 retry)");
        _output.WriteLine("[Atteso] InvalidOperationException subito: l'errore di input non viene ritentato né mascherato");

        var stub = new StubHandler(_ => new HttpResponseMessage(HttpStatusCode.OK));
        using var client = new HttpClient(stub);
        var helper = new httpsClientHelper(client, new HttpRequestEvents(), null!)
            .addRetryCondition(r => !r.IsSuccessStatusCode, 2, 0);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => helper.SendAsync(null!, HttpMethod.Get));
        _output.WriteLine($"[Restituito] {ex.GetType().Name}: {ex.Message}");
    }

    [Fact]
    public async Task SendAsync_RateLimiterEnabled_AddsElapsedHeaderAndAllowsWithinLimit() {
        _output.WriteLine("[Scenario] Rate limiter attivo (1 richiesta all'ora): prima richiesta GET");
        _output.WriteLine("[Atteso] 200 OK e la richiesta contiene l'header X-RateLimit-TimeSpanElapsed");

        var stub = new StubHandler(_ => new HttpResponseMessage(HttpStatusCode.OK));
        using var client = new HttpClient(stub);
        var helper = new httpsClientHelper(client, new HttpRequestEvents(), OneRequestPerHour());

        using var response = await helper.SendAsync(Url, HttpMethod.Get);

        _output.WriteLine($"[Restituito] Status={(int)response.StatusCode}, HeaderElapsed={stub.LastRequest!.Headers.Contains("X-RateLimit-TimeSpanElapsed")}");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.True(stub.LastRequest!.Headers.Contains("X-RateLimit-TimeSpanElapsed"));
    }

    [Fact]
    public async Task SendAsync_RateLimitExceeded_Returns429WithoutCallingHandler() {
        _output.WriteLine("[Scenario] Rate limiter attivo (1 richiesta all'ora): due richieste GET consecutive");
        _output.WriteLine("[Atteso] La seconda riceve 429 Too Many Requests senza chiamare l'handler (Calls resta 1)");

        var stub = new StubHandler(_ => new HttpResponseMessage(HttpStatusCode.OK));
        using var client = new HttpClient(stub);
        var helper = new httpsClientHelper(client, new HttpRequestEvents(), OneRequestPerHour());
        using var first = await helper.SendAsync(Url, HttpMethod.Get);

        using var second = await helper.SendAsync(Url, HttpMethod.Get);

        _output.WriteLine($"[Restituito] Seconda={(int)second.StatusCode}, ChiamateHandler={stub.Calls}");
        Assert.Equal(HttpStatusCode.TooManyRequests, second.StatusCode);
        Assert.Equal(1, stub.Calls);
    }

    [Fact]
    public async Task SendAsync_RateLimiterDisabled_DoesNotLimit() {
        _output.WriteLine("[Scenario] Opzioni di rate limit con IsEnabled=false (limite di 1 richiesta all'ora ignorato): due richieste GET");
        _output.WriteLine("[Atteso] Entrambe passano: la seconda è 200 OK e l'handler viene chiamato 2 volte");

        var stub = new StubHandler(_ => new HttpResponseMessage(HttpStatusCode.OK));
        using var client = new HttpClient(stub);
        var options = OneRequestPerHour();
        options.IsEnabled = false;
        var helper = new httpsClientHelper(client, new HttpRequestEvents(), options);

        using var first = await helper.SendAsync(Url, HttpMethod.Get);
        using var second = await helper.SendAsync(Url, HttpMethod.Get);

        _output.WriteLine($"[Restituito] Seconda={(int)second.StatusCode}, ChiamateHandler={stub.Calls}");
        Assert.Equal(HttpStatusCode.OK, second.StatusCode);
        Assert.Equal(2, stub.Calls);
    }

    [Fact]
    public async Task SendAsync_CallerCancelled_ThrowsOperationCanceledException() {
        _output.WriteLine("[Scenario] CancellationToken già cancellato passato a SendAsync");
        _output.WriteLine("[Atteso] OperationCanceledException propagata al chiamante (non trasformata in 408)");

        var handler = new AsyncStubHandler((_, ct) => Task.FromCanceled<HttpResponseMessage>(ct));
        using var client = new HttpClient(handler);
        var helper = new httpsClientHelper(client, new HttpRequestEvents(), null!);
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        var ex = await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => helper.SendAsync(Url, HttpMethod.Get, cancellationToken: cts.Token));
        _output.WriteLine($"[Restituito] {ex.GetType().Name}");
    }

    [Fact]
    public async Task SendAsync_CallerCancelledDuringRequest_ThrowsOperationCanceledException() {
        _output.WriteLine("[Scenario] Handler che non risponde mai; il chiamante cancella il token dopo 50 ms durante la richiesta");
        _output.WriteLine("[Atteso] OperationCanceledException propagata al chiamante");

        var handler = new AsyncStubHandler(async (_, ct) => {
            await Task.Delay(Timeout.InfiniteTimeSpan, ct);
            return new HttpResponseMessage(HttpStatusCode.OK);
        });
        using var client = new HttpClient(handler);
        var helper = new httpsClientHelper(client, new HttpRequestEvents(), null!);
        using var cts = new CancellationTokenSource();
        cts.CancelAfter(TimeSpan.FromMilliseconds(50));

        var ex = await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => helper.SendAsync(Url, HttpMethod.Get, cancellationToken: cts.Token));
        _output.WriteLine($"[Restituito] {ex.GetType().Name}");
    }

    [Fact]
    public async Task SendAsync_HttpClientTimeoutWithoutAddTimeout_Returns408() {
        _output.WriteLine("[Scenario] HttpClient con Timeout di 50 ms, handler che non risponde mai e nessun addTimeout sull'helper");
        _output.WriteLine("[Atteso] Il timeout dell'HttpClient viene tradotto in 408 Request Timeout");

        var handler = new AsyncStubHandler(async (_, ct) => {
            await Task.Delay(Timeout.InfiniteTimeSpan, ct);
            return new HttpResponseMessage(HttpStatusCode.OK);
        });
        using var client = new HttpClient(handler) { Timeout = TimeSpan.FromMilliseconds(50) };
        var helper = new httpsClientHelper(client, new HttpRequestEvents(), null!);

        using var response = await helper.SendAsync(Url, HttpMethod.Get);

        _output.WriteLine($"[Restituito] Status={(int)response.StatusCode}");
        Assert.Equal(HttpStatusCode.RequestTimeout, response.StatusCode);
    }

    [Fact]
    public async Task AddTimeout_AfterFirstSend_DoesNotThrow() {
        _output.WriteLine("[Scenario] Invio una prima richiesta e solo dopo chiamo addTimeout(5 s) sull'helper");
        _output.WriteLine("[Atteso] Nessuna eccezione (es. \"properties can only be set before the first request\")");

        var stub = new StubHandler(_ => new HttpResponseMessage(HttpStatusCode.OK));
        using var client = new HttpClient(stub);
        var helper = new httpsClientHelper(client, new HttpRequestEvents(), null!);
        using var first = await helper.SendAsync(Url, HttpMethod.Get);

        var ex = Record.Exception(() => helper.addTimeout(TimeSpan.FromSeconds(5)));

        _output.WriteLine($"[Restituito] Eccezione={ex?.GetType().Name ?? "nessuna"}");
        Assert.Null(ex);
    }

    [Fact]
    public async Task AddTimeout_CalledTwice_LastValueWins() {
        _output.WriteLine("[Scenario] addTimeout(5 minuti) e poi addTimeout(50 ms); handler che non risponde mai");
        _output.WriteLine("[Atteso] Vince l'ultimo valore: la richiesta scade dopo ~50 ms con 408 Request Timeout");

        var handler = new AsyncStubHandler(async (_, ct) => {
            await Task.Delay(Timeout.InfiniteTimeSpan, ct);
            return new HttpResponseMessage(HttpStatusCode.OK);
        });
        using var client = new HttpClient(handler);
        var helper = new httpsClientHelper(client, new HttpRequestEvents(), null!)
            .addTimeout(TimeSpan.FromMinutes(5))
            .addTimeout(TimeSpan.FromMilliseconds(50));

        using var response = await helper.SendAsync(Url, HttpMethod.Get);

        _output.WriteLine($"[Restituito] Status={(int)response.StatusCode}");
        Assert.Equal(HttpStatusCode.RequestTimeout, response.StatusCode);
    }

    [Fact]
    public async Task SendAsync_CallerOwnedClient_ShorterClientTimeoutWinsAndReturns408() {
        _output.WriteLine("[Scenario] HttpClient del chiamante con Timeout 30 ms e helper con addTimeout(5 s); handler che non risponde mai");
        _output.WriteLine("[Atteso] Vince il timeout più corto (30 ms del client): 408 Request Timeout");

        var handler = new AsyncStubHandler(async (_, ct) => {
            await Task.Delay(Timeout.InfiniteTimeSpan, ct);
            return new HttpResponseMessage(HttpStatusCode.OK);
        });
        using var client = new HttpClient(handler) { Timeout = TimeSpan.FromMilliseconds(30) };
        var helper = new httpsClientHelper(client, new HttpRequestEvents(), null!)
            .addTimeout(TimeSpan.FromSeconds(5));

        using var response = await helper.SendAsync(Url, HttpMethod.Get);

        _output.WriteLine($"[Restituito] Status={(int)response.StatusCode}");
        Assert.Equal(HttpStatusCode.RequestTimeout, response.StatusCode);
    }

    [Fact]
    public async Task SendAsync_AfterCompletion_DisposesRequestAndRetryClones() {
        _output.WriteLine("[Scenario] POST JSON con 1 retry (stub: 500 e poi 200): al termine verifico le richieste viste dall'handler");
        _output.WriteLine("[Atteso] Memory leak check: le 2 richieste (originale + clone del retry) risultano disposte (ObjectDisposedException leggendo il Content)");

        var seen = new List<HttpRequestMessage>();
        var statuses = new Queue<HttpStatusCode>([HttpStatusCode.InternalServerError, HttpStatusCode.OK]);
        var stub = new StubHandler(r => { seen.Add(r); return new HttpResponseMessage(statuses.Dequeue()); });
        using var client = new HttpClient(stub);
        var helper = new httpsClientHelper(client, new HttpRequestEvents(), null!)
            .addRetryCondition(r => !r.IsSuccessStatusCode, 2, 0);

        using var response = await helper.SendAsync(Url, HttpMethod.Post, new JsonContentBuilder(), "{\"id\":1}");

        var disposed = 0;
        foreach (var request in seen) {
            await Assert.ThrowsAsync<ObjectDisposedException>(() => request.Content!.ReadAsStringAsync());
            disposed++;
        }
        _output.WriteLine($"[Restituito] RichiesteViste={seen.Count}, ConContentDisposto={disposed}");

        Assert.Equal(2, seen.Count);
        Assert.Equal(seen.Count, disposed);
    }

    [Fact]
    public async Task Ctor_HttpClientAlreadyUsed_DoesNotThrow() {
        _output.WriteLine("[Scenario] Costruisco httpsClientHelper su un HttpClient che ha già inviato una richiesta");
        _output.WriteLine("[Atteso] Nessuna eccezione nel costruttore (non prova a modificare un client già usato)");

        var stub = new StubHandler(_ => new HttpResponseMessage(HttpStatusCode.OK));
        using var client = new HttpClient(stub);
        using var warmup = await client.GetAsync(Url);

        var ex = Record.Exception(() => new httpsClientHelper(client, new HttpRequestEvents(), null!));

        _output.WriteLine($"[Restituito] Eccezione={ex?.GetType().Name ?? "nessuna"}");
        Assert.Null(ex);
    }

    [Fact]
    public async Task SendAsync_CallerCancelledWithRetryPolicy_ThrowsWithoutRetrying() {
        _output.WriteLine("[Scenario] Token già cancellato con politica di retry attiva (3 retry su !IsSuccessStatusCode)");
        _output.WriteLine("[Atteso] OperationCanceledException senza ritentare: l'handler viene chiamato al massimo 1 volta");

        var handler = new AsyncStubHandler((_, ct) => Task.FromCanceled<HttpResponseMessage>(ct));
        using var client = new HttpClient(handler);
        var helper = new httpsClientHelper(client, new HttpRequestEvents(), null!)
            .addRetryCondition(r => !r.IsSuccessStatusCode, 3, 0);
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        var ex = await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => helper.SendAsync(Url, HttpMethod.Get, cancellationToken: cts.Token));
        _output.WriteLine($"[Restituito] {ex.GetType().Name}, ChiamateHandler={handler.Calls}");
        Assert.True(handler.Calls <= 1);
    }

    [Fact]
    public async Task SendAsync_HandlerThrowsGenericException_Returns500() {
        _output.WriteLine("[Scenario] L'handler lancia InvalidOperationException(\"boom\")");
        _output.WriteLine("[Atteso] 500 Internal Server Error con \"internal_error\" nel body (l'eccezione non esce dall'helper)");

        var stub = new StubHandler(_ => throw new InvalidOperationException("boom"));
        using var client = new HttpClient(stub);
        var helper = new httpsClientHelper(client, new HttpRequestEvents(), null!);

        using var response = await helper.SendAsync(Url, HttpMethod.Get);

        _output.WriteLine($"[Restituito] Status={(int)response.StatusCode}");
        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
        Assert.Contains("internal_error", await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task SendAsync_HelperTimeoutElapses_Returns408() {
        _output.WriteLine("[Scenario] addTimeout(50 ms) sull'helper; handler che non risponde mai");
        _output.WriteLine("[Atteso] 408 Request Timeout");

        var handler = new AsyncStubHandler(async (_, ct) => {
            await Task.Delay(Timeout.InfiniteTimeSpan, ct);
            return new HttpResponseMessage(HttpStatusCode.OK);
        });
        using var client = new HttpClient(handler);
        var helper = new httpsClientHelper(client, new HttpRequestEvents(), null!)
            .addTimeout(TimeSpan.FromMilliseconds(50));

        using var response = await helper.SendAsync(Url, HttpMethod.Get);

        _output.WriteLine($"[Restituito] Status={(int)response.StatusCode}");
        Assert.Equal(HttpStatusCode.RequestTimeout, response.StatusCode);
    }

    [Fact]
    public void Ctor_CallerOwnedClient_TimeoutIsNotChanged() {
        _output.WriteLine("[Scenario] HttpClient del chiamante con Timeout 7 s: creo due helper (uno con addTimeout 1 s, uno senza)");
        _output.WriteLine("[Atteso] Il Timeout dell'HttpClient resta 7 s (l'helper non modifica client che non possiede)");

        using var client = new HttpClient(new StubHandler(_ => new HttpResponseMessage(HttpStatusCode.OK))) { Timeout = TimeSpan.FromSeconds(7) };

        _ = new httpsClientHelper(client, new HttpRequestEvents(), null!).addTimeout(TimeSpan.FromSeconds(1));
        _ = new httpsClientHelper(client, new HttpRequestEvents(), null!);

        _output.WriteLine($"[Restituito] Timeout={client.Timeout}");
        Assert.Equal(TimeSpan.FromSeconds(7), client.Timeout);
    }

    [Fact]
    public async Task SendAsync_SecondHelperOnSharedClient_KeepsClientTimeout() {
        _output.WriteLine("[Scenario] Due helper sullo stesso HttpClient (Timeout 50 ms); invio con il secondo; handler che non risponde mai");
        _output.WriteLine("[Atteso] Il secondo helper non altera il timeout del client condiviso: 408 Request Timeout dopo ~50 ms");

        var handler = new AsyncStubHandler(async (_, ct) => {
            await Task.Delay(Timeout.InfiniteTimeSpan, ct);
            return new HttpResponseMessage(HttpStatusCode.OK);
        });
        using var client = new HttpClient(handler) { Timeout = TimeSpan.FromMilliseconds(50) };
        _ = new httpsClientHelper(client, new HttpRequestEvents(), null!);
        var second = new httpsClientHelper(client, new HttpRequestEvents(), null!);

        using var response = await second.SendAsync(Url, HttpMethod.Get);

        _output.WriteLine($"[Restituito] Status={(int)response.StatusCode}");
        Assert.Equal(HttpStatusCode.RequestTimeout, response.StatusCode);
    }

    [Fact]
    public async Task SendAsync_CallbackCancelsItself_ReturnsServerResponse() {
        _output.WriteLine("[Scenario] Callback di HttpRequestEvents che restituisce un Task già cancellato; il server risponde 200");
        _output.WriteLine("[Atteso] La cancellazione interna della callback non annulla la richiesta: 200 OK");

        var events = new HttpRequestEvents();
        events.Add((_, _, _, _) => Task.FromCanceled(new CancellationToken(true)));
        using var client = new HttpClient(new HttpClientHandlerLogging(events) {
            InnerHandler = new StubHandler(_ => new HttpResponseMessage(HttpStatusCode.OK))
        });
        var helper = new httpsClientHelper(client, events, null!);

        using var response = await helper.SendAsync(Url, HttpMethod.Get);

        _output.WriteLine($"[Restituito] Status={(int)response.StatusCode}");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task AddFormData_KeyValuePairs_BuildsFormUrlEncodedContent() {
        _output.WriteLine("[Scenario] addFormData con le coppie a=1 e b=2");
        _output.WriteLine("[Atteso] Il contenuto è form-url-encoded: \"a=1&b=2\"");

        using var client = new HttpClient(new StubHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)));
        var helper = new httpsClientHelper(client, new HttpRequestEvents(), null!);

        helper.addFormData([new("a", "1"), new("b", "2")]);

        var content = await helper.formUrlEncodedContent.ReadAsStringAsync();
        _output.WriteLine($"[Restituito] Contenuto=\"{content}\"");

        Assert.Equal("a=1&b=2", content);
    }

    [Fact]
    public async Task SetHeadersAndBearerAuthenticationSync_DefaultHeaders_SentOnRequest() {
        _output.WriteLine("[Scenario] setHeadersAndBearerAuthenticationSync con header di default X-Default=d e Bearer \"tok\"; GET");
        _output.WriteLine("[Atteso] La richiesta contiene X-Default=\"d\" e Authorization \"Bearer tok\"");

        var stub = new StubHandler(_ => new HttpResponseMessage(HttpStatusCode.OK));
        using var client = new HttpClient(stub);
        var helper = new httpsClientHelper(client, new HttpRequestEvents(), null!)
            .setHeadersAndBearerAuthenticationSync(new Dictionary<string, string> { ["X-Default"] = "d" }, new httpsClientHelper.httpClientAuthenticationBearer("tok"));

        using var response = await helper.SendAsync(Url, HttpMethod.Get);

        _output.WriteLine($"[Restituito] X-Default=[{string.Join(", ", stub.LastRequest!.Headers.GetValues("X-Default"))}], Authorization={stub.LastRequest.Headers.Authorization}");
        Assert.Equal("d", Assert.Single(stub.LastRequest!.Headers.GetValues("X-Default")));
        Assert.Equal("Bearer tok", stub.LastRequest.Headers.Authorization!.ToString());
    }

    [Fact]
    public async Task SetHeadersWithoutAuthorizationSync_CalledTwice_ReplacesPreviousDefaults() {
        _output.WriteLine("[Scenario] setHeadersWithoutAuthorizationSync chiamato due volte: prima X-Old=1, poi X-New=2");
        _output.WriteLine("[Atteso] La seconda chiamata sostituisce i default precedenti: X-Old assente, X-New presente");

        var stub = new StubHandler(_ => new HttpResponseMessage(HttpStatusCode.OK));
        using var client = new HttpClient(stub);
        var helper = new httpsClientHelper(client, new HttpRequestEvents(), null!)
            .setHeadersWithoutAuthorizationSync(new Dictionary<string, string> { ["X-Old"] = "1" })
            .setHeadersWithoutAuthorizationSync(new Dictionary<string, string> { ["X-New"] = "2" });

        using var response = await helper.SendAsync(Url, HttpMethod.Get);

        _output.WriteLine($"[Restituito] X-Old presente={stub.LastRequest!.Headers.Contains("X-Old")}, X-New presente={stub.LastRequest.Headers.Contains("X-New")}");
        Assert.False(stub.LastRequest!.Headers.Contains("X-Old"));
        Assert.True(stub.LastRequest.Headers.Contains("X-New"));
    }

    private static httpClientRateLimitOptions OneRequestPerHour() => new() {
        IsEnabled = true,
        PermitLimit = 1,
        QueueLimit = 0,
        SegmentsPerWindow = 1,
        Window = TimeSpan.FromHours(1),
        AutoReplenishment = false
    };

    private sealed class AsyncStubHandler : HttpMessageHandler {
        private readonly Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> _responder;
        public AsyncStubHandler(Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> responder) => _responder = responder;
        public int Calls { get; private set; }
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) {
            Calls++;
            return _responder(request, cancellationToken);
        }
    }

    private sealed class StubHandler : HttpMessageHandler {
        private readonly Func<HttpRequestMessage, HttpResponseMessage> _responder;
        public StubHandler(Func<HttpRequestMessage, HttpResponseMessage> responder) => _responder = responder;
        public int Calls { get; private set; }
        public HttpRequestMessage? LastRequest { get; private set; }
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) {
            Calls++;
            LastRequest = request;
            return Task.FromResult(_responder(request));
        }
    }
}
