namespace CSharpEssentials.HttpHelper.HttpMocks;
//Interface to inject
public interface IHttpMockScenario {
    Func<HttpRequestMessage, bool> Match { get; }
    IReadOnlyList<Func<Task<HttpResponseMessage>>> ResponseFactory { get; }
}