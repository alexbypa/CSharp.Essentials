namespace CSharpEssentials.HttpHelper.HttpMocks;
/// <summary>Mock scenario that can be injected into the mock engine: a request matcher plus the responses to return.</summary>
public interface IHttpMockScenario {
    Func<HttpRequestMessage, bool> Match { get; }
    IReadOnlyList<Func<Task<HttpResponseMessage>>> ResponseFactory { get; }
    /// <summary>Factories that receive the request and the cancellation token; when non-empty, they take precedence over <see cref="ResponseFactory"/>. Default: empty. Custom implementations must declare it as a public (or explicit interface) member, otherwise the default applies.</summary>
    IReadOnlyList<Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>>> RequestResponseFactory => [];
}
