﻿using Polly;
using Polly.Retry;
using System.Text;
using System.Threading.RateLimiting;
using static CSharpEssentials.HttpHelper.httpsClientHelper;

namespace CSharpEssentials.HttpHelper;
public class httpsClientHelper : IhttpsClientHelper {
    protected readonly IHttpClientFactory clientFactory;
    protected HttpClient httpClient;
    protected HttpClientHandlerLogging httpLoggingHandler;
    protected RateLimiter rateLimiter;
    public object JsonData { get; set; }
    public FormUrlEncodedContent formUrlEncodedContent { get; set; }
    private AsyncRetryPolicy<HttpResponseMessage> _retryPolicy = null;
    private readonly IHttpRequestEvents _events;
    private bool TimeoutSettled = false;
    public record httpClientAuthenticationBasic(string userName, string password);
    public record httpClientAuthenticationBearer(string token); 
    public httpsClientHelper(HttpClient httpClient, IHttpRequestEvents events, httpClientRateLimitOptions rateLimitOptions) {
        this.httpClient = httpClient;
        _events = events;

        if (rateLimitOptions != null)
        rateLimiter = new SlidingWindowRateLimiter(new SlidingWindowRateLimiterOptions {
            AutoReplenishment = rateLimitOptions.AutoReplenishment,
            PermitLimit = rateLimitOptions.PermitLimit,
            QueueLimit = rateLimitOptions.QueueLimit,
            SegmentsPerWindow = rateLimitOptions.SegmentsPerWindow,
            Window = rateLimitOptions.Window
        });
    }
    public IhttpsClientHelper AddRequestAction(Func<HttpRequestMessage, HttpResponseMessage, int, TimeSpan, Task> action) {
        _events.Add(action);
        return this;
    }
    public IhttpsClientHelper addFormData(List<KeyValuePair<string, string>> keyValuePairs) {
        formUrlEncodedContent = new FormUrlEncodedContent(keyValuePairs);
        return this;
    }
    public IhttpsClientHelper addRetryCondition(Func<HttpResponseMessage, bool> RetryCondition, int retryCount, double backoffFactor) {
        _retryPolicy = Policy
                    .Handle<HttpRequestException>()
                    .OrResult<HttpResponseMessage>(RetryCondition)
                    .WaitAndRetryAsync(retryCount,
                        retryAttempt => TimeSpan.FromSeconds(Math.Pow(backoffFactor, retryAttempt)),
                        onRetry: (outcome, timespan, attempt, context) => {
                            context["RetryAttempt"] = attempt;
                        }
                     )

                    ;
        return this;
    }
    private void _setHeaders(Dictionary<string, string> _HeaderValues) {
        httpClient.DefaultRequestHeaders.Clear();
        if (_HeaderValues != null)
            foreach (var item in _HeaderValues) {
                httpClient.DefaultRequestHeaders.Add(item.Key, item.Value);
            }
    }
    public IhttpsClientHelper setHeadersWithoutAuthorization(Dictionary<string, string> _HeaderValues) {
        _setHeaders(_HeaderValues);
        return this;
    }
    public IhttpsClientHelper setHeadersAndBearerAuthentication(Dictionary<string, string> _HeaderValues, httpClientAuthenticationBearer httpClientAuthenticationBearer) {
        _setHeaders(_HeaderValues);
        httpClient.DefaultRequestHeaders.Add("Authorization", "Bearer " + httpClientAuthenticationBearer.token);
        return this;
    }
    public IhttpsClientHelper setHeadersAndBasicAuthentication(Dictionary<string, string> _HeaderValues, httpClientAuthenticationBasic httpClientAuthenticationBasic) {
        _setHeaders(_HeaderValues);
        String encoded = Convert.ToBase64String(Encoding.GetEncoding("ISO-8859-1").GetBytes(httpClientAuthenticationBasic.userName + ":" + httpClientAuthenticationBasic.password));
        httpClient.DefaultRequestHeaders.Add("Authorization", "Basic " + encoded);
        return this;
    }
    public async Task<HttpResponseMessage> SendAsync(
    string baseUrl,
    HttpMethod httpMethod,
    object body,
    IContentBuilder contentBuilder) {
        var request = new HttpRequestBuilder()
            .WithUrl(baseUrl)
            .WithMethod(httpMethod)
            .WithBody(body)
            .WithContentBuilder(contentBuilder)
            .Build();

        DateTime dtStartRequest = DateTime.Now;
        TimeSpan timeSpanRateLimit = TimeSpan.Zero;
        if (rateLimiter != null) {
            var lease = await rateLimiter.AcquireAsync(1);
            if (!lease.IsAcquired) {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("[RateLimit] BLOCCATA la richiesta.");
                Console.ResetColor();
                throw new InvalidOperationException("Rate limit exceeded");
            }
            if (request.Headers.Contains("X-RateLimit-TimeSpanElapsed"))
                request.Headers.Remove("X-RateLimit-TimeSpanElapsed");
            request.Headers.Add("X-RateLimit-TimeSpanElapsed", (DateTime.Now - dtStartRequest).ToString());
        }
        var context = new Context();
        Task<HttpResponseMessage> response = _retryPolicy == null ? httpClient.SendAsync(request) : _retryPolicy.ExecuteAsync(
            async ctx => {
                var attempt = ctx.ContainsKey("RetryAttempt") ? (int)ctx["RetryAttempt"] : 0;

                if (request.Headers.Contains("X-Retry-Attempt"))
                    request.Headers.Remove("X-Retry-Attempt");

                request.Headers.Add("X-Retry-Attempt", attempt.ToString());

                return await httpClient.SendAsync(CloneHttpRequestMessage(request));
            }, context);
        //HttpResponseMessage response = await httpClient.SendAsync(request);

        return await response;
    }
    private static HttpRequestMessage CloneHttpRequestMessage(HttpRequestMessage request) {
        var clone = new HttpRequestMessage(request.Method, request.RequestUri);

        // Copia gli header
        foreach (var header in request.Headers) {
            clone.Headers.TryAddWithoutValidation(header.Key, header.Value);
        }

        // Clona il contenuto (se presente)
        if (request.Content != null) {
            var content = request.Content.ReadAsByteArrayAsync().Result; // Blocca solo in questa fase per la clonazione
            clone.Content = new ByteArrayContent(content);

            // Copia gli header del contenuto
            foreach (var header in request.Content.Headers) {
                clone.Content.Headers.TryAddWithoutValidation(header.Key, header.Value);
            }
        }
        return clone;
    }
    public IhttpsClientHelper addTimeout(TimeSpan timeSpan) {
        if (!TimeoutSettled)
            httpClient.Timeout = timeSpan;
        TimeoutSettled = true;
        return this;
    }
    public IhttpsClientHelper addHeaders(string KeyName, string KeyValue) {
        httpClient.DefaultRequestHeaders.Add(KeyName, KeyValue);
        return this;
    }
}
public interface IhttpsClientHelper {
    Task<HttpResponseMessage> SendAsync(
        string baseUrl,
        HttpMethod httpMethod,
        object body,
        IContentBuilder contentBuilder);
    IhttpsClientHelper AddRequestAction(Func<HttpRequestMessage, HttpResponseMessage, int, TimeSpan, Task> action);
    IhttpsClientHelper addFormData(List<KeyValuePair<string, string>> keyValuePairs);
    IhttpsClientHelper addRetryCondition(Func<HttpResponseMessage, bool> RetryCondition, int retryCount, double backoffFactor);
    IhttpsClientHelper addTimeout(TimeSpan timeSpan);
    IhttpsClientHelper addHeaders(string KeyName, string KeyValue);
    IhttpsClientHelper setHeadersWithoutAuthorization(Dictionary<string, string> _HeaderValues);
    IhttpsClientHelper setHeadersAndBearerAuthentication(Dictionary<string, string> _HeaderValues, httpClientAuthenticationBearer httpClientAuthenticationBearer);
    IhttpsClientHelper setHeadersAndBasicAuthentication(Dictionary<string, string> _HeaderValues, httpClientAuthenticationBasic httpClientAuthenticationBasic);
}