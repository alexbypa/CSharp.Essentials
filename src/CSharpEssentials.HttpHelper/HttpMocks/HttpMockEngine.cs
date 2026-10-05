namespace CSharpEssentials.HttpHelper.HttpMocks;
public interface IHttpMockEngine {
    IEnumerable<IHttpMockScenario> scenarios { get; }
    bool Match(HttpRequestMessage request);
    HttpMessageHandler Build();
}
/// <summary>
/// Routes requests to the matching scenario (last match wins) via an internal <see cref="HttpMessageHandler"/>.
/// </summary>
public class HttpMockEngine : IHttpMockEngine {
    public IEnumerable<IHttpMockScenario> scenarios { get; }
    private HttpMessageHandler? _cachedHandler;
    public bool Match(HttpRequestMessage request) {
        bool isMatched = false;
        foreach (var scenario in scenarios) {
            if (scenario.Match.Invoke(request)) {
                isMatched = true;
                break;
            }
        }
        return isMatched;
    }
    public HttpMockEngine(IEnumerable<IHttpMockScenario> httpMockScenarios) {
        scenarios = httpMockScenarios;
    }
    public HttpMessageHandler Build() => _cachedHandler ??= new ScenarioHandler(scenarios.ToArray());

    /// <summary>Last matching scenario wins (same order as Moq setups); no match → <see cref="InvalidOperationException"/>.</summary>
    private sealed class ScenarioHandler(IHttpMockScenario[] scenarios) : HttpMessageHandler {
        private readonly long[] _counters = new long[scenarios.Length];

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) {
            for (var i = scenarios.Length - 1; i >= 0; i--) {
                if (!scenarios[i].Match(request)) continue;
                // ponytail: concrete-type check keeps IHttpMockScenario unchanged; custom implementations get WaitAsync but not the token in the factory; upgrade path = default interface member
                var requestFactories = scenarios[i] is HttpMockScenario { RequestResponseFactory.Count: > 0 } s ? s.RequestResponseFactory : null;
                var factories = scenarios[i].ResponseFactory;
                var count = requestFactories?.Count ?? factories.Count;
                if (count == 0) throw new InvalidOperationException($"Mock scenario for {request.Method} {request.RequestUri} has no ResponseFactory.");
                // Round-robin over the factories; Interlocked keeps the cursor correct under concurrent requests.
                var next = (int)((Interlocked.Increment(ref _counters[i]) - 1) % count);
                return requestFactories is not null
                    ? requestFactories[next](request, cancellationToken).WaitAsync(cancellationToken)
                    : factories[next]().WaitAsync(cancellationToken);
            }
            throw new InvalidOperationException($"No mock scenario matches {request.Method} {request.RequestUri}.");
        }
    }
    public HttpMessageHandler Orchestrate() {
        return Build();
    }

}
