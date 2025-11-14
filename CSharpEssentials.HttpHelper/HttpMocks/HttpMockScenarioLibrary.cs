using System.Net;

namespace CSharpEssentials.HttpHelper.HttpMocks;
public static class HttpMockScenarioLibrary {
    public static IHttpMockScenario InternalErrorThenOk(string pattern) =>
        new HttpMockScenario(
        req => req.RequestUri.AbsolutePath.Contains(pattern),
        new List<Func<Task<HttpResponseMessage>>>{
            () => Task.FromResult(new HttpResponseMessage(HttpStatusCode.InternalServerError)),
            () => Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK))
        });
    public static IHttpMockScenario HostNotFound(string pattern) =>
        new HttpMockScenario(
            req => req.RequestUri.AbsolutePath.Contains(pattern),
            new List<Func<Task<HttpResponseMessage>>> {
               () => throw new HttpRequestException("no such host is know")
        });
}