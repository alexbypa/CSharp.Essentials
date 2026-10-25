using Xunit.Abstractions;
using System.Net;

namespace CSharpEssentials.HttpHelper.Tests;

/// <summary>
/// Scenario "pagamento accettato, risposta persa, poi retry": verifica cosa succede al downstream provider
/// (non al marcatore sintetico, già coperto da <see cref="HttpResponseMessageExtensionsTests"/>) quando il retry
/// dell'helper ripete una richiesta che il provider ha già eseguito prima che la risposta si perdesse.
/// </summary>
public class PaymentIdempotencyRetryTests {
    private readonly ITestOutputHelper _output;

    public PaymentIdempotencyRetryTests(ITestOutputHelper output) {
        _output = output;
    }

    private const string Url = "http://stub.local/api/payments";

    [Fact]
    public async Task Retry_WithStableIdempotencyKey_RecordsSingleProviderEffect() {
        _output.WriteLine("[Scenario] POST pagamento con Idempotency-Key stabile; il 1° tentativo registra il pagamento sul provider e poi perde la risposta (HttpRequestException), il 2° tentativo risponde 200 OK");
        _output.WriteLine("[Atteso] Il provider registra un solo effetto (stessa chiave nei due tentativi); il 1° tentativo catturato dal retry ha IsTransportFailure()==true; risposta finale 200 OK");

        var provider = new FakePaymentProvider();
        const string stableKey = "pay-123";
        var calls = 0;
        var stub = new StubHandler(r => {
            calls++;
            var key = r.Headers.TryGetValues("Idempotency-Key", out var values) ? values.First() : null;
            provider.Charge(key);
            if (calls == 1)
                throw new HttpRequestException("connection lost after charge");
            return new HttpResponseMessage(HttpStatusCode.OK);
        });
        using var client = new HttpClient(stub);
        var capturedOutcomes = new List<HttpResponseMessage>();
        var helper = new httpsClientHelper(client, new HttpRequestEvents(), null!)
            .addRetryCondition(r => { capturedOutcomes.Add(r); return r.IsTransportFailure(); }, 3, 0);

        using var response = await helper.SendAsync(Url, HttpMethod.Post,
            headers: new Dictionary<string, string> { ["Idempotency-Key"] = stableKey });

        _output.WriteLine($"[Restituito] ChiamateHandler={calls}, EffettiProvider={provider.Charges.Count}, ChiaviViste=[{string.Join(", ", provider.ChargeAttempts)}], PrimoTentativo.IsTransportFailure={capturedOutcomes[0].IsTransportFailure()}, StatusFinale={(int)response.StatusCode}");
        Assert.Single(provider.Charges);
        Assert.Equal(2, provider.ChargeAttempts.Count);
        Assert.Equal(stableKey, provider.ChargeAttempts[0]);
        Assert.Equal(stableKey, provider.ChargeAttempts[1]);
        Assert.True(capturedOutcomes[0].IsTransportFailure());
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Retry_WithoutIdempotencyKey_RecordsDoubleProviderEffect() {
        _output.WriteLine("[Scenario] Stesso scenario del test precedente ma senza header Idempotency-Key");
        _output.WriteLine("[Atteso] Il provider registra due effetti distinti: senza una chiave stabile il retry non è riconoscibile come lo stesso pagamento (rischio di doppio addebito, non risolto da questo test)");

        var provider = new FakePaymentProvider();
        var calls = 0;
        var stub = new StubHandler(r => {
            calls++;
            var key = r.Headers.TryGetValues("Idempotency-Key", out var values) ? values.First() : null;
            provider.Charge(key);
            if (calls == 1)
                throw new HttpRequestException("connection lost after charge");
            return new HttpResponseMessage(HttpStatusCode.OK);
        });
        using var client = new HttpClient(stub);
        var capturedOutcomes = new List<HttpResponseMessage>();
        var helper = new httpsClientHelper(client, new HttpRequestEvents(), null!)
            .addRetryCondition(r => { capturedOutcomes.Add(r); return r.IsTransportFailure(); }, 3, 0);

        using var response = await helper.SendAsync(Url, HttpMethod.Post);

        _output.WriteLine($"[Restituito] ChiamateHandler={calls}, EffettiProvider={provider.Charges.Count}, PrimoTentativo.IsTransportFailure={capturedOutcomes[0].IsTransportFailure()}, StatusFinale={(int)response.StatusCode}");
        Assert.Equal(2, provider.Charges.Count);
        Assert.Equal(2, provider.ChargeAttempts.Count);
        Assert.All(provider.ChargeAttempts, k => Assert.Null(k));
        Assert.True(capturedOutcomes[0].IsTransportFailure());
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    /// <summary>
    /// Fake downstream payment provider: non è codice di produzione, vive solo in questo file di test.
    /// Registra ogni tentativo di addebito (<see cref="ChargeAttempts"/>) e conta gli effetti monetari distinti
    /// che avrebbe prodotto (<see cref="Charges"/>), raggruppati per Idempotency-Key. Quando la chiave manca,
    /// ogni chiamata è trattata come un nuovo addebito (nessun modo di riconoscere il retry come ripetizione).
    /// </summary>
    private sealed class FakePaymentProvider {
        public List<string?> ChargeAttempts { get; } = [];
        public Dictionary<string, int> Charges { get; } = [];

        public void Charge(string? idempotencyKey) {
            ChargeAttempts.Add(idempotencyKey);
            var effectiveKey = idempotencyKey ?? Guid.NewGuid().ToString();
            Charges[effectiveKey] = Charges.GetValueOrDefault(effectiveKey) + 1;
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
