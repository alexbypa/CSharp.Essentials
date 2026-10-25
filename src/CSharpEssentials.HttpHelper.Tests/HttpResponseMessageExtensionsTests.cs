using Xunit.Abstractions;
using System.Net;

namespace CSharpEssentials.HttpHelper.Tests;

public class HttpResponseMessageExtensionsTests {
    private readonly ITestOutputHelper _output;

    public HttpResponseMessageExtensionsTests(ITestOutputHelper output) {
        _output = output;
    }

    private const string Url = "http://stub.local/api";

    [Fact]
    public async Task GetSyntheticReason_RealBadGatewayFromServer_ReturnsNull() {
        _output.WriteLine("[Scenario] Lo stub risponde realmente 502 Bad Gateway senza lanciare eccezioni");
        _output.WriteLine("[Atteso] IsSynthetic=false, IsTransportFailure=false, GetSyntheticReason=null (risposta reale del server, non sintetica)");

        var stub = new StubHandler(_ => new HttpResponseMessage(HttpStatusCode.BadGateway));
        using var client = new HttpClient(stub);
        var helper = new httpsClientHelper(client, new HttpRequestEvents(), null!);

        using var response = await helper.SendAsync(Url, HttpMethod.Get);

        _output.WriteLine($"[Restituito] Status={(int)response.StatusCode}, IsSynthetic={response.IsSynthetic()}, IsTransportFailure={response.IsTransportFailure()}, Reason={response.GetSyntheticReason()}");
        Assert.False(response.IsSynthetic());
        Assert.False(response.IsTransportFailure());
        Assert.Null(response.GetSyntheticReason());
    }

    [Fact]
    public async Task GetSyntheticReason_HandlerThrowsHttpRequestException_ReturnsTransportFailure() {
        _output.WriteLine("[Scenario] L'handler lancia HttpRequestException(\"boom\")");
        _output.WriteLine("[Atteso] 502 Bad Gateway sintetico con reason TransportFailure; IsTransportFailure=true");

        var stub = new StubHandler(_ => throw new HttpRequestException("boom"));
        using var client = new HttpClient(stub);
        var helper = new httpsClientHelper(client, new HttpRequestEvents(), null!);

        using var response = await helper.SendAsync(Url, HttpMethod.Get);

        _output.WriteLine($"[Restituito] Status={(int)response.StatusCode}, IsSynthetic={response.IsSynthetic()}, IsTransportFailure={response.IsTransportFailure()}, Reason={response.GetSyntheticReason()}");
        Assert.Equal(HttpStatusCode.BadGateway, response.StatusCode);
        Assert.True(response.IsSynthetic());
        Assert.True(response.IsTransportFailure());
        Assert.Equal(SyntheticResponseReason.TransportFailure, response.GetSyntheticReason());
    }

    [Fact]
    public async Task GetSyntheticReason_HelperTimeoutElapses_ReturnsTimeout() {
        _output.WriteLine("[Scenario] addTimeout(50 ms) sull'helper; handler che non risponde mai");
        _output.WriteLine("[Atteso] 408 Request Timeout sintetico con reason Timeout; IsTransportFailure=true");

        var handler = new AsyncStubHandler(async (_, ct) => {
            await Task.Delay(Timeout.InfiniteTimeSpan, ct);
            return new HttpResponseMessage(HttpStatusCode.OK);
        });
        using var client = new HttpClient(handler);
        var helper = new httpsClientHelper(client, new HttpRequestEvents(), null!)
            .addTimeout(TimeSpan.FromMilliseconds(50));

        using var response = await helper.SendAsync(Url, HttpMethod.Get);

        _output.WriteLine($"[Restituito] Status={(int)response.StatusCode}, IsSynthetic={response.IsSynthetic()}, IsTransportFailure={response.IsTransportFailure()}, Reason={response.GetSyntheticReason()}");
        Assert.Equal(HttpStatusCode.RequestTimeout, response.StatusCode);
        Assert.True(response.IsSynthetic());
        Assert.True(response.IsTransportFailure());
        Assert.Equal(SyntheticResponseReason.Timeout, response.GetSyntheticReason());
    }

    [Fact]
    public async Task GetSyntheticReason_RateLimiterRejectsLease_ReturnsRateLimited() {
        _output.WriteLine("[Scenario] Rate limiter attivo (1 richiesta all'ora): due richieste GET consecutive");
        _output.WriteLine("[Atteso] La seconda riceve 429 sintetico con reason RateLimited; IsTransportFailure=false (la richiesta non è mai stata inviata)");

        var stub = new StubHandler(_ => new HttpResponseMessage(HttpStatusCode.OK));
        using var client = new HttpClient(stub);
        var helper = new httpsClientHelper(client, new HttpRequestEvents(), OneRequestPerHour());
        using var first = await helper.SendAsync(Url, HttpMethod.Get);

        using var second = await helper.SendAsync(Url, HttpMethod.Get);

        _output.WriteLine($"[Restituito] Status={(int)second.StatusCode}, IsSynthetic={second.IsSynthetic()}, IsTransportFailure={second.IsTransportFailure()}, Reason={second.GetSyntheticReason()}");
        Assert.Equal(HttpStatusCode.TooManyRequests, second.StatusCode);
        Assert.True(second.IsSynthetic());
        Assert.False(second.IsTransportFailure());
        Assert.Equal(SyntheticResponseReason.RateLimited, second.GetSyntheticReason());
    }

    [Fact]
    public async Task GetSyntheticReason_HandlerThrowsGenericException_ReturnsInternalError() {
        _output.WriteLine("[Scenario] L'handler lancia InvalidOperationException(\"boom\")");
        _output.WriteLine("[Atteso] 500 Internal Server Error sintetico con reason InternalError; IsTransportFailure=false (esito non di trasporto)");

        var stub = new StubHandler(_ => throw new InvalidOperationException("boom"));
        using var client = new HttpClient(stub);
        var helper = new httpsClientHelper(client, new HttpRequestEvents(), null!);

        using var response = await helper.SendAsync(Url, HttpMethod.Get);

        _output.WriteLine($"[Restituito] Status={(int)response.StatusCode}, IsSynthetic={response.IsSynthetic()}, IsTransportFailure={response.IsTransportFailure()}, Reason={response.GetSyntheticReason()}");
        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
        Assert.True(response.IsSynthetic());
        Assert.False(response.IsTransportFailure());
        Assert.Equal(SyntheticResponseReason.InternalError, response.GetSyntheticReason());
    }

    [Fact]
    public async Task GetSyntheticReason_RetryClonesDisposedRequest_MarkerSurvivesAndFinalResponseIsReal() {
        _output.WriteLine("[Scenario] Retry (backoff 0) su IsTransportFailure(): primo tentativo lancia HttpRequestException, secondo risponde 200 reale");
        _output.WriteLine("[Atteso] Il predicato di retry legge il marcatore dal clone già disposto (TransportFailure sul primo tentativo); la risposta finale 200 non è sintetica");

        var capturedOutcomes = new List<HttpResponseMessage>();
        var calls = 0;
        var stub = new StubHandler(_ => {
            calls++;
            if (calls == 1)
                throw new HttpRequestException("boom");
            return new HttpResponseMessage(HttpStatusCode.OK);
        });
        using var client = new HttpClient(stub);
        var helper = new httpsClientHelper(client, new HttpRequestEvents(), null!)
            .addRetryCondition(r => { capturedOutcomes.Add(r); return r.IsTransportFailure(); }, 1, 0);

        using var response = await helper.SendAsync(Url, HttpMethod.Get);

        _output.WriteLine($"[Restituito] TentativiCatturati={capturedOutcomes.Count}, PrimoTentativo.IsTransportFailure={capturedOutcomes[0].IsTransportFailure()}, StatusFinale={(int)response.StatusCode}, Finale.IsSynthetic={response.IsSynthetic()}");
        Assert.Equal(2, capturedOutcomes.Count);
        Assert.True(capturedOutcomes[0].IsTransportFailure());
        Assert.Equal(SyntheticResponseReason.TransportFailure, capturedOutcomes[0].GetSyntheticReason());
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.False(response.IsSynthetic());
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
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) {
            return _responder(request, cancellationToken);
        }
    }

    private sealed class StubHandler : HttpMessageHandler {
        private readonly Func<HttpRequestMessage, HttpResponseMessage> _responder;
        public StubHandler(Func<HttpRequestMessage, HttpResponseMessage> responder) => _responder = responder;
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) {
            return Task.FromResult(_responder(request));
        }
    }
}
