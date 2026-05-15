using Moq;
using Moq.Protected;

namespace CSharpEssentials.HttpHelper.HttpMocks;
public interface IHttpMockEngine {
    IEnumerable<IHttpMockScenario> scenarios { get; }
    bool Match(HttpRequestMessage request);
    HttpMessageHandler Build();
}
/// <summary>
/// Real Moq
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
    public HttpMessageHandler Build() {
        if (_cachedHandler != null)
            return _cachedHandler;

        var mock = new Mock<HttpMessageHandler>(MockBehavior.Strict);

        foreach (var scenario in scenarios) {
            // Inizializziamo l'indice locale per lo scenario
            int index = 0;

            mock.Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.Is<HttpRequestMessage>(r => scenario.Match(r)),
                    ItExpr.IsAny<CancellationToken>())
                .Returns(() => {
                    // Preleviamo la factory all'indice corrente
                    var factory = scenario.ResponseFactory[index];

                    // Incrementiamo l'indice e usiamo il modulo per farlo tornare a 0 
                    // quando raggiunge la fine della lista (scenario.ResponseFactory.Count)
                    index = (index + 1) % scenario.ResponseFactory.Count;

                    return factory();
                });
        }

        _cachedHandler = mock.Object;
        return _cachedHandler;

        //foreach (var scenario in scenarios) {

        //    var seq = mock.Protected()
        //        .SetupSequence<Task<HttpResponseMessage>>(
        //            "SendAsync",
        //            ItExpr.Is<HttpRequestMessage>(r => scenario.Match(r)),
        //            ItExpr.IsAny<CancellationToken>());

        //    foreach (var responseFactory in scenario.ResponseFactory)
        //        seq.Returns(responseFactory);
        //}
        //return mock.Object;
    }
    public HttpMessageHandler Orchestrate() {
        return Build();
    }

}
