﻿using System.Text;
using System.Threading.RateLimiting;

namespace CSharpEssentials.HttpHelper;
public class HttpClientHandlerLogging : DelegatingHandler {
    private readonly IHttpRequestEvents _events;
    public HttpClientHandlerLogging(IHttpRequestEvents events) => _events = events; 
    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) {
        string pageCalled = GetPageName(request);

        HttpResponseMessage response = null;
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
        
        await _events.InvokeAll(request, response, totRetry, RateLimitTimeSpanElapsed);
        //_events.ClearAll();
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

public class HttpRequestEvents : IHttpRequestEvents {
    private readonly List<Func<HttpRequestMessage, HttpResponseMessage, int, TimeSpan, Task>> _callbacks = new();
    public void Add(Func<HttpRequestMessage, HttpResponseMessage, int, TimeSpan, Task> c) => _callbacks.Add(c);
    public void ClearAll() {
        _callbacks.Clear();
    }
    public Task InvokeAll(HttpRequestMessage request, HttpResponseMessage response, int retryCount, TimeSpan rateLimitTimeSpanElapsed) {
        var tasks = _callbacks
            .Select(cb => cb(request, response, retryCount, rateLimitTimeSpanElapsed));
        
        return Task.WhenAll(tasks);
    }
}