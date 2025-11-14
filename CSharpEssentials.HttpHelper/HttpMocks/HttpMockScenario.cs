namespace CSharpEssentials.HttpHelper.HttpMocks;
//DTO
public class HttpMockScenario : IHttpMockScenario {
    public Func<HttpRequestMessage, bool> Match { get; }
    public IReadOnlyList<Func<Task<HttpResponseMessage>>> ResponseFactory { get; }
    public HttpMockScenario(Func<HttpRequestMessage, bool> match, List<Func<Task<HttpResponseMessage>>> responseFactory) {
        Match = match;
        ResponseFactory = responseFactory;
    }
}