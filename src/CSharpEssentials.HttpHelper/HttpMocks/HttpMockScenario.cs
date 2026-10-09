namespace CSharpEssentials.HttpHelper.HttpMocks;
//DTO
public class HttpMockScenario : IHttpMockScenario {
    public Func<HttpRequestMessage, bool> Match { get; }
    /// <summary>Parameterless factories. Scenarios built with the (request, <see cref="CancellationToken"/>) constructor expose an empty list here and fill <see cref="RequestResponseFactory"/> instead.</summary>
    public IReadOnlyList<Func<Task<HttpResponseMessage>>> ResponseFactory { get; }
    /// <summary>Factories that receive the request and the cancellation token (timeout / caller cancellation). Takes precedence over <see cref="ResponseFactory"/>.</summary>
    public IReadOnlyList<Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>>> RequestResponseFactory { get; } = [];
    /// <summary>Creates a scenario whose factories take no arguments.</summary>
    public HttpMockScenario(Func<HttpRequestMessage, bool> match, List<Func<Task<HttpResponseMessage>>> responseFactory) {
        Match = match;
        ResponseFactory = responseFactory;
    }
    /// <summary>Creates a scenario whose factories receive the request and the cancellation token (timeout / caller cancellation), so mocks can honour them.</summary>
    public HttpMockScenario(Func<HttpRequestMessage, bool> match, List<Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>>> responseFactory) {
        Match = match;
        ResponseFactory = [];
        RequestResponseFactory = responseFactory;
    }
}