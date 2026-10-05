using System.Text;

namespace CSharpEssentials.HttpHelper;
public class HttpClientHandlerLogging : DelegatingHandler {
    private readonly IHttpRequestEvents _events;
    private readonly IHttpRequestEvents? _globalEvents;
    public HttpClientHandlerLogging(IHttpRequestEvents events) => _events = events;
    public HttpClientHandlerLogging(IHttpRequestEvents events, IHttpRequestEvents? globalEvents) : this(events) => _globalEvents = globalEvents;
    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) {
        string pageCalled = GetPageName(request);

        HttpResponseMessage response = null;
        StringBuilder requestLog = new StringBuilder();
        StringBuilder responseLog = new StringBuilder();
        requestLog.Append(request.ToString());
        if (request.Content != null) {
            requestLog.Append(await request.Content.ReadAsStringAsync());
        }

        int totRetry = request.Headers.Contains("X-Retry-Attempt") ? int.Parse(request.Headers.GetValues("X-Retry-Attempt").FirstOrDefault()) : 0;
        TimeSpan RateLimitTimeSpanElapsed = request.Headers.Contains("X-RateLimit-TimeSpanElapsed") ? TimeSpan.Parse(request.Headers.GetValues("X-RateLimit-TimeSpanElapsed").FirstOrDefault()) : TimeSpan.Zero;

        response = await base.SendAsync(request, cancellationToken);
        try {
            await _events.InvokeAll(request, response, totRetry, RateLimitTimeSpanElapsed);
            if (_globalEvents != null && !ReferenceEquals(_globalEvents, _events))
                await _globalEvents.InvokeAll(request, response, totRetry, RateLimitTimeSpanElapsed);
        } catch (OperationCanceledException ex) when (!cancellationToken.IsCancellationRequested) {
            // Annullamento interno a una callback (non della richiesta): non deve diventare un finto timeout 408.
            HttpHelperLog.Write(Serilog.Events.LogEventLevel.Error, ex, "HttpRequestEvents callback was cancelled and was ignored");
        }
        return response;
    }
    public static string GetPageName(HttpRequestMessage request) {
        if (request == null || request.RequestUri == null)
            return null;

        return request.RequestUri.PathAndQuery;
    }
}

public interface IHttpRequestEvents {
    void Add(Func<HttpRequestMessage, HttpResponseMessage, int, TimeSpan, Task> callback);
    Task InvokeAll(HttpRequestMessage req, HttpResponseMessage res, int totRetry, TimeSpan RateLimitTimeSpanElapsed);
    void ClearAll();
}

/// <summary>Thread-safe callback list: <see cref="InvokeAll"/> runs on a snapshot, so Add/Clear can happen concurrently.</summary>
public class HttpRequestEvents : IHttpRequestEvents {
    private readonly object _gate = new();
    private Func<HttpRequestMessage, HttpResponseMessage, int, TimeSpan, Task>[] _callbacks = [];
    public void Add(Func<HttpRequestMessage, HttpResponseMessage, int, TimeSpan, Task> c) {
        lock (_gate) _callbacks = [.. _callbacks, c];
    }
    public void ClearAll() {
        lock (_gate) _callbacks = [];
    }
    public Task InvokeAll(HttpRequestMessage request, HttpResponseMessage response, int retryCount, TimeSpan rateLimitTimeSpanElapsed) {
        var snapshot = Volatile.Read(ref _callbacks);
        var tasks = snapshot.Select(cb => InvokeIsolated(cb, request, response, retryCount, rateLimitTimeSpanElapsed));

        return Task.WhenAll(tasks);
    }
    // A throwing callback (sync or async) must not fail the HTTP response nor stop the other callbacks.
    private static async Task InvokeIsolated(Func<HttpRequestMessage, HttpResponseMessage, int, TimeSpan, Task> callback, HttpRequestMessage request, HttpResponseMessage response, int retryCount, TimeSpan rateLimitTimeSpanElapsed) {
        try {
            await callback(request, response, retryCount, rateLimitTimeSpanElapsed);
        } catch (Exception ex) when (ex is not OperationCanceledException) { // cancellation is not a callback failure
            HttpHelperLog.Write(Serilog.Events.LogEventLevel.Error, ex, "HttpRequestEvents callback failed and was ignored");
        }
    }
}

/// <summary>
/// Holds one <see cref="IHttpRequestEvents"/> per named client, so a callback registered on one
/// client's helper fires only for that client's requests.
/// </summary>
public sealed class HttpRequestEventsRegistry {
    private readonly System.Collections.Concurrent.ConcurrentDictionary<string, IHttpRequestEvents> _events = new();
    public IHttpRequestEvents For(string clientName) => _events.GetOrAdd(clientName, _ => new HttpRequestEvents());
}
