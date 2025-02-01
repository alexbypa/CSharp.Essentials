using System.Text;

namespace CSharpEssentials.HttpHelper;
public class HttpClientHandlerLogging : DelegatingHandler {
    public List<Func<HttpRequestMessage, HttpResponseMessage, Task>> _RequestActions = new();
    public HttpClientHandlerLogging(List<Func<HttpRequestMessage, HttpResponseMessage, Task>> RequestActions) {
        _RequestActions = RequestActions;
    }
    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) {
        string pageCalled = GetPageName(request);
        
        DateTime timeRequest = DateTime.Now;
        StringBuilder requestLog = new StringBuilder();
        DateTime dtStartRequest = DateTime.Now;
        var now = DateTimeOffset.Now;
        StringBuilder responseLog = new StringBuilder();
        DateTime dtEndRequest = DateTime.Now;
        requestLog.Append(request.ToString());
        if (request.Content != null) {
            requestLog.Append(await request.Content.ReadAsStringAsync());
        }
        var response = await base.SendAsync(request, cancellationToken);
        foreach (var action in _RequestActions) {
            await action.Invoke(request, response);
        }
        return response;
    }
    public static string GetPageName(HttpRequestMessage request) {
        if (request == null || request.RequestUri == null)
            return null;

        return request.RequestUri.PathAndQuery;
    }
}
