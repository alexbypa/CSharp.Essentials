using System.Text.Json;
using System.Text;
using System.Threading.RateLimiting;

namespace CSharpEssentials.HttpHelper;
public class httpsClientHelper : IhttpsClientHelper {
    protected readonly IHttpClientFactory clientFactory;
    protected HttpClient httpClient;
    protected HttpClientHandlerLogging httpLoggingHandler;
    protected RateLimiter rateLimiter;
    private Func<HttpResponseMessage, bool> RetryCondition;
    public object JsonData { get; set; }
    public FormUrlEncodedContent formUrlEncodedContent { get; set; }
    public httpsClientHelper(List<Func<HttpRequestMessage, HttpResponseMessage, Task>> actions) {
        httpClient = new HttpClient(new HttpClientHandlerLogging(actions) {
            InnerHandler = new HttpClientHandler()
        });
    }
    public httpsClientHelper(IHttpClientFactory clientFactory, string clientName) {
        this.clientFactory = clientFactory;
        httpClient = this.clientFactory.CreateClient(clientName);
    }

    public httpsClientHelper(IHttpClientFactory clientFactory, string clientName, Func<HttpResponseMessage, bool> RetryCondition) {
        this.clientFactory = clientFactory;
        httpClient = this.clientFactory.CreateClient(clientName);
        this.RetryCondition = RetryCondition;
    }
    public httpsClientHelper addFormData(List<KeyValuePair<string, string>> keyValuePairs) {
        formUrlEncodedContent = new FormUrlEncodedContent(keyValuePairs);
        return this;
    }
    public async Task<HttpResponseMessage> sendAsync(string BaseUrl) {
        if (rateLimiter != null) {
            RateLimitLease lease = await rateLimiter.AcquireAsync();
        }
        Task<HttpResponseMessage> response = null;

        if (formUrlEncodedContent != null) {
            response = httpClient.PostAsync(BaseUrl, formUrlEncodedContent);
        } else {
            if (JsonData != null && !(JsonData is string)) {
                var settings = new JsonSerializerOptions { DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull };
                var json = JsonSerializer.Serialize(JsonData, settings);
                StringContent content = new StringContent(json, Encoding.UTF8, "application/json");
                response = httpClient.PostAsync(BaseUrl, content);
            } else if (JsonData is string) {
                StringContent content = new StringContent((string)JsonData, Encoding.UTF8, "application/json");
                response = httpClient.PostAsync(BaseUrl, content);
            } else {
                response = httpClient.GetAsync(BaseUrl);
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