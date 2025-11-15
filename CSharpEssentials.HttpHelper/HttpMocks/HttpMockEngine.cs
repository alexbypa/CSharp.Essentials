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
