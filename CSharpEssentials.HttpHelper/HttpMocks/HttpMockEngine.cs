using Moq;
using Moq.Protected;

namespace CSharpEssentials.HttpHelper.HttpMocks;
public interface IHttpMockEngine {
    IEnumerable<IHttpMockScenario> scenarios { get; }
    bool Match(HttpRequestMessage request);
}
/// <summary>
/// Real Moq
/// </summary>
public class HttpMockEngine : IHttpMockEngine {
    public IEnumerable<IHttpMockScenario> scenarios { get; }
    public bool Match(HttpRequestMessage request) {
        return true; // per adesso restituiamo sempre true !
    }

    public HttpMockEngine(IEnumerable<IHttpMockScenario> httpMockScenarios) {
        scenarios = httpMockScenarios;
    }
    private HttpMessageHandler Build() {
        var mock = new Mock<HttpMessageHandler>(MockBehavior.Strict);
        foreach (var scenario in scenarios) {
            var seq = mock.Protected()
                .SetupSequence<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.Is<HttpRequestMessage>(r => scenario.Match(r)),
                    ItExpr.IsAny<CancellationToken>());

            foreach (var responseFactory in scenario.ResponseFactory)
                seq.Returns(responseFactory);
        }
        return mock.Object;
    }

    public HttpMessageHandler Orchestrate() {
        return Build();
    }

}
