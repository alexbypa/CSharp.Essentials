using System.Text.Json;
using System.Text;
using System.Threading.RateLimiting;
using Polly;
using Polly.Retry;
using System.Reflection;
using System;
using System.Reflection.Metadata;
using System.Net.Http;

namespace CSharpEssentials.HttpHelper;
public class httpsClientHelper : IhttpsClientHelper {
    protected readonly IHttpClientFactory clientFactory;
    protected HttpClient httpClient;
    protected HttpClientHandlerLogging httpLoggingHandler;
    protected RateLimiter rateLimiter;
    public object JsonData { get; set; }
    public FormUrlEncodedContent formUrlEncodedContent { get; set; }
    private AsyncRetryPolicy<HttpResponseMessage> _retryPolicy = null;

    public httpsClientHelper(List<Func<HttpRequestMessage, HttpResponseMessage, int, TimeSpan, Task>> actions) {
        httpClient = new HttpClient(new HttpClientHandlerLogging() {
            _RequestActions = actions,
            InnerHandler = new HttpClientHandler()
        });
    }
    public httpsClientHelper(IHttpClientFactory clientFactory, string clientName) {
        this.clientFactory = clientFactory;
        httpClient = this.clientFactory.CreateClient(clientName);
    }
    public httpsClientHelper addFormData(List<KeyValuePair<string, string>> keyValuePairs) {
        formUrlEncodedContent = new FormUrlEncodedContent(keyValuePairs);
        return this;
    }
    public httpsClientHelper addRetryCondition(Func<HttpResponseMessage, bool> RetryCondition, int retryCount, double backoffFactor) {
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
            await rateLimiter.AcquireAsync();
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

    /// <summary>
    /// deprecated
    /// </summary>
    /// <param name="BaseUrl"></param>
    /// <returns></returns>
    private async Task<HttpResponseMessage> sendAsync(string BaseUrl) {
        if (rateLimiter != null) {
            RateLimitLease lease = await rateLimiter.AcquireAsync();
        }
        Task<HttpResponseMessage> response = null;

        if (formUrlEncodedContent != null) {
            response = _retryPolicy == null ? httpClient.PostAsync(BaseUrl, formUrlEncodedContent) : _retryPolicy.ExecuteAsync(() => httpClient.PostAsync(BaseUrl, formUrlEncodedContent));

        } else {
            if (JsonData != null && !(JsonData is string)) {
                var settings = new JsonSerializerOptions { DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull };
                var json = JsonSerializer.Serialize(JsonData, settings);
                StringContent content = new StringContent(json, Encoding.UTF8, "application/json");
                response = _retryPolicy == null ? httpClient.PostAsync(BaseUrl, content) : _retryPolicy.ExecuteAsync(() => httpClient.PostAsync(BaseUrl, content));
            } else if (JsonData is string) {
                StringContent content = new StringContent((string)JsonData, Encoding.UTF8, "application/json");
                response = _retryPolicy == null ? response = httpClient.PostAsync(BaseUrl, content) : _retryPolicy.ExecuteAsync(() => httpClient.PostAsync(BaseUrl, content));
            } else {
                response = _retryPolicy == null ? response = response = httpClient.GetAsync(BaseUrl) : _retryPolicy.ExecuteAsync(() => response = httpClient.GetAsync(BaseUrl));
            }
        }
        return await response;
    }
    public httpsClientHelper addTimeout(TimeSpan timeSpan) {
        httpClient.Timeout = timeSpan;
        return this;
    }
    public httpsClientHelper addHeaders(string KeyName, string KeyValue) {
        httpClient.DefaultRequestHeaders.Add(KeyName, KeyValue);
        return this;
    }
    public httpsClientHelper addRateLimitOnMoreRequests(httpClientRateLimitOptions rateLimitOptions) {
        if (rateLimitOptions != null)
            rateLimiter = new SlidingWindowRateLimiter(new SlidingWindowRateLimiterOptions {
                AutoReplenishment = rateLimitOptions.AutoReplenishment,
                PermitLimit = rateLimitOptions.PermitLimit,
                QueueLimit = rateLimitOptions.QueueLimit,
                SegmentsPerWindow = rateLimitOptions.SegmentsPerWindow,
                Window = rateLimitOptions.Window
            });
        return this;
    }

    public object addRateLimit(httpClientOptions httpClientOptions) {
        throw new NotImplementedException();
    }
}
public interface IhttpsClientHelper { }