using System.Text;
using System.Threading.RateLimiting;

namespace CSharpEssentials.HttpHelper;
public class HttpClientHandlerLogging : DelegatingHandler {
    public List<Func<HttpRequestMessage, HttpResponseMessage, int, TimeSpan, Task>> _RequestActions = new();
    public RateLimiter _rateLimiter;
    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) {
        string pageCalled = GetPageName(request);

        //bool IsAcquired = true;
        //if (_rateLimiter != null) {
        //    using RateLimitLease lease = await _rateLimiter.AcquireAsync(permitCount: 1, cancellationToken);
        //    IsAcquired = lease.IsAcquired;
        //}

        HttpResponseMessage response = null;
        //if (IsAcquired) {
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
        response = await base.SendAsync(request, cancellationToken);
        int totRetry = request.Headers.Contains("X-Retry-Attempt") ? int.Parse(request.Headers.GetValues("X-Retry-Attempt").FirstOrDefault()) : 0;
        TimeSpan RateLimitTimeSpanElapsed = request.Headers.Contains("X-RateLimit-TimeSpanElapsed") ? TimeSpan.Parse(request.Headers.GetValues("X-RateLimit-TimeSpanElapsed").FirstOrDefault()) : TimeSpan.Zero;

        foreach (var action in _RequestActions) {
            await action.Invoke(request, response, totRetry, RateLimitTimeSpanElapsed);
        }
        //}
        return response;
    }
    public static string GetPageName(HttpRequestMessage request) {
        if (request == null || request.RequestUri == null)
            return null;

        return request.RequestUri.PathAndQuery;
    }
}
