using System.Text.Json;
using System.Text;
using System.Threading.RateLimiting;
using Polly;
using Polly.Retry;
using System.Reflection;
using System;
using System.Reflection.Metadata;

namespace CSharpEssentials.HttpHelper;
public class httpsClientHelper : IhttpsClientHelper {
    protected readonly IHttpClientFactory clientFactory;
    protected HttpClient httpClient;
    protected HttpClientHandlerLogging httpLoggingHandler;
    protected RateLimiter rateLimiter;
    public object JsonData { get; set; }
    public FormUrlEncodedContent formUrlEncodedContent { get; set; }
    private AsyncRetryPolicy<HttpResponseMessage> _retryPolicy = null;

    public httpsClientHelper(List<Func<HttpRequestMessage, HttpResponseMessage, Task>> actions) {
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
                    .OrResult<HttpResponseMessage>(RetryCondition) // Riprova solo per 5xx
                    .WaitAndRetryAsync(retryCount, retryAttempt =>
                        TimeSpan.FromSeconds(Math.Pow(backoffFactor, retryAttempt))
                     );
        return this;
    }
    public async Task<HttpResponseMessage> sendAsync(string BaseUrl) {
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
    public httpsClientHelper addRateLimit(httpClientRateLimitOptions rateLimitOptions) {
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
}
public interface IhttpsClientHelper { }