using System.Net;
namespace CSharpEssentials.HttpHelper.HttpMocks;
public static class HttpMockScenarioLibrary {
    //TODO: cambiare pattern in predicate !
    public static IHttpMockScenario InternalErrorThenOk(string pattern) =>
        new HttpMockScenario(
        req => req.RequestUri.AbsoluteUri.Contains(pattern, StringComparison.InvariantCultureIgnoreCase),
        new List<Func<Task<HttpResponseMessage>>>{
            () => Task.FromResult(new HttpResponseMessage(HttpStatusCode.InternalServerError){ Content = new StringContent("Bravo sei passato dal Moq ;) ")}),
            () => Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK))
        });
    public static IHttpMockScenario HostNotFound(string pattern) =>
        new HttpMockScenario(
            req => req.RequestUri.AbsoluteUri.Contains(pattern, StringComparison.InvariantCultureIgnoreCase),
            new List<Func<Task<HttpResponseMessage>>> {
               () => throw new HttpRequestException("no such host is know")
        });
    public static IHttpMockScenario OkWithCustomBody(string pattern) =>
        new HttpMockScenario(
            req => req.RequestUri.AbsoluteUri.Contains(pattern, StringComparison.InvariantCultureIgnoreCase),
            new List<Func<Task<HttpResponseMessage>>> {
               () => Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK){Content = new StringContent("\"result\" : \"Test Moq: ok\"") }) 
        });
}