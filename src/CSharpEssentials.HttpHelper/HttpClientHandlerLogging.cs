using System.Diagnostics;
using Microsoft.Extensions.Options;
using Serilog;
using Serilog.Events;

namespace CSharpEssentials.HttpHelper;
public class HttpClientHandlerLogging : DelegatingHandler {
    private readonly IHttpRequestEvents _events;
    private readonly IHttpRequestEvents? _globalEvents;
    private readonly string _clientName = "";
    private readonly IOptionsMonitor<HttpHelperLoggingOptions>? _logging;
    private readonly Serilog.ILogger? _logger;
    public HttpClientHandlerLogging(IHttpRequestEvents events) => _events = events;
    public HttpClientHandlerLogging(IHttpRequestEvents events, IHttpRequestEvents? globalEvents) : this(events) => _globalEvents = globalEvents;
    /// <summary>Handler with built-in request logging (status, elapsed, attempt, CorrelationId), for hand-built pipelines.</summary>
    /// <param name="events">Callbacks invoked after each response.</param>
    /// <param name="globalEvents">Optional second callback list (e.g. registered globally).</param>
    /// <param name="clientName">Matched ordinal against <c>LogRequests</c> ("*" or client names).</param>
    /// <param name="logging">Logging options (<c>LogRequests</c>, <c>CorrelationIdHeader</c>).</param>
    /// <param name="logger">Target for request events; null = LoggerHelper pipeline / <c>Log.Logger</c>.
    /// With an injected logger the events do not carry ApplicationName / Action="HttpHelper".</param>
    public HttpClientHandlerLogging(IHttpRequestEvents events, IHttpRequestEvents? globalEvents, string clientName, IOptionsMonitor<HttpHelperLoggingOptions> logging, Serilog.ILogger? logger = null) : this(events, globalEvents) {
        ArgumentNullException.ThrowIfNull(clientName);
        ArgumentNullException.ThrowIfNull(logging);
        _clientName = clientName;
        _logging = logging;
        _logger = logger;
    }
    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) {
        int totRetry = request.Headers.TryGetValues("X-Retry-Attempt", out var retryValues) && int.TryParse(retryValues.FirstOrDefault(), out var retry) ? retry : 0;
        TimeSpan RateLimitTimeSpanElapsed = request.Headers.TryGetValues("X-RateLimit-TimeSpanElapsed", out var waitValues) && TimeSpan.TryParse(waitValues.FirstOrDefault(), out var waited) ? waited : TimeSpan.Zero;

        var opts = _logging?.CurrentValue;
        bool log = opts is not null && ShouldLog(opts.LogRequests, _clientName);
        string? cid = opts?.CorrelationIdHeader is { Length: > 0 } h ? EnsureCorrelationId(request, h) : null;
        long start = log ? Stopwatch.GetTimestamp() : 0;

        HttpResponseMessage response;
        try {
            response = await base.SendAsync(request, cancellationToken);
        } catch (Exception ex) when (log) {
            LogFailure(request, ex, start, totRetry, cid);
            throw;
        }
        if (log)
            LogResponse(request, response, start, totRetry, cid);
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
    private static bool ShouldLog(List<string> logRequests, string clientName) =>
        logRequests.Count > 0 && (logRequests.Contains("*") || logRequests.Contains(clientName, StringComparer.Ordinal));

    /// <summary>Set by <see cref="httpsClientHelper"/> before sending: lets the handler tell a caller cancel from a timeout (the token it receives is a merged one).</summary>
    internal static readonly HttpRequestOptionsKey<CancellationToken> CallerTokenKey = new("CSharpEssentials.HttpHelper.CallerToken");
    internal static readonly HttpRequestOptionsKey<KeyValuePair<string, string>> GeneratedCorrelationKey = new("CSharpEssentials.HttpHelper.GeneratedCorrelation");

    // Caller value wins; otherwise generate and remember it in Options so the retry loop can carry it to the next attempt.
    private static string? EnsureCorrelationId(HttpRequestMessage request, string headerName) {
        if (request.Headers.NonValidated.TryGetValues(headerName, out var existing))
            return existing.ToString();
        var id = Activity.Current is { IdFormat: ActivityIdFormat.W3C } a ? a.TraceId.ToHexString() : Guid.NewGuid().ToString("N");
        if (!request.Headers.TryAddWithoutValidation(headerName, id))
            return null; // invalid or content header name: nothing sent, so nothing to log or carry over retries
        request.Options.Set(GeneratedCorrelationKey, new KeyValuePair<string, string>(headerName, id));
        return id;
    }

    private void LogResponse(HttpRequestMessage request, HttpResponseMessage response, long start, int attempt, string? cid) {
        int status = (int)response.StatusCode;
        var level = status >= 500 || status == 408 ? LogEventLevel.Error
            : status >= 400 ? LogEventLevel.Warning
            : LogEventLevel.Information;
        WriteEvent(level, null, cid, "HTTP {Method} {RequestPath} responded {StatusCode} in {ElapsedMs} ms (attempt {RetryAttempt})",
            request.Method.Method, GetRequestPath(request), status, ElapsedMs(start), attempt);
    }

    private void LogFailure(HttpRequestMessage request, Exception ex, long start, int attempt, string? cid) {
        // Key present: a cancel not requested by the caller is a timeout → Error. Without the key (plain HttpClient / hand-built pipeline) the two are indistinguishable: Warning.
        var timedOut = request.Options.TryGetValue(CallerTokenKey, out var caller) && !caller.IsCancellationRequested;
        var level = ex is OperationCanceledException && !timedOut ? LogEventLevel.Warning : LogEventLevel.Error;
        WriteEvent(level, ex, cid, "HTTP {Method} {RequestPath} failed in {ElapsedMs} ms (attempt {RetryAttempt})",
            request.Method.Method, GetRequestPath(request), ElapsedMs(start), attempt);
    }

    private void WriteEvent(LogEventLevel level, Exception? ex, string? cid, string template, params object?[] args) {
        var l = _logger ?? HttpHelperLog.Logger;
        if (!l.IsEnabled(level))
            return;
        l = l.ForContext("HttpClientName", _clientName);
        if (cid is not null)
            l = l.ForContext("CorrelationId", cid);
        l.Write(level, ex, template, args);
    }

    private static double ElapsedMs(long start) => Math.Round(Stopwatch.GetElapsedTime(start).TotalMilliseconds, 1);

    // Path only: query string may carry secrets.
    private static string GetRequestPath(HttpRequestMessage request) {
        var uri = request.RequestUri;
        if (uri is null)
            return "";
        if (uri.IsAbsoluteUri)
            return uri.AbsolutePath;
        var s = uri.OriginalString;
        int q = s.IndexOf('?');
        return q < 0 ? s : s[..q];
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
