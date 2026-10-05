using Polly;
using Polly.Retry;
using System.Text;
using System.Threading;
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
    private TimeSpan _timeout;
    public record httpClientAuthenticationBasic(string userName, string password);
    public record httpClientAuthenticationBearer(string token);
    public httpsClientHelper(HttpClient httpClient, IHttpRequestEvents events, httpClientRateLimitOptions rateLimitOptions) {
        this.httpClient = httpClient;
        _events = events;

        // L'HttpClient appartiene al chiamante: non lo si modifica (può essere condiviso). Senza addTimeout vale il suo
        // HttpClient.Timeout; con addTimeout vale il più corto dei due. Entrambi i timeout diventano 408 in _SendAsync.
        // La factory, proprietaria dei client che crea, azzera HttpClient.Timeout e passa il default via addTimeout.
        _timeout = Timeout.InfiniteTimeSpan;

        if (rateLimitOptions != null && rateLimitOptions.IsEnabled)
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
                            context["BackoffTime"] = timespan; // Salva il tempo di attesa nel contesto Polly
                        }
                     );
        return this;
    }
    private void _setHeaders(Dictionary<string, string> _HeaderValues) {
        httpClient.DefaultRequestHeaders.Clear();
        if (_HeaderValues != null)
            foreach (var item in _HeaderValues) {
                httpClient.DefaultRequestHeaders.Add(item.Key, item.Value);
            }
    }
    public IhttpsClientHelper setHeadersWithoutAuthorizationSync(Dictionary<string, string> _HeaderValues) {
        _setHeaders(_HeaderValues);
        return this;
    }
    public IhttpsClientHelper setHeadersAndBearerAuthenticationSync(Dictionary<string, string> _HeaderValues, httpClientAuthenticationBearer httpClientAuthenticationBearer) {
        _setHeaders(_HeaderValues);
        httpClient.DefaultRequestHeaders.Add("Authorization", "Bearer " + httpClientAuthenticationBearer.token);
        return this;
    }
    public IhttpsClientHelper setHeadersAndBasicAuthenticationSync(Dictionary<string, string> _HeaderValues, httpClientAuthenticationBasic httpClientAuthenticationBasic) {
        _setHeaders(_HeaderValues);
        String encoded = Convert.ToBase64String(Encoding.GetEncoding("ISO-8859-1").GetBytes(httpClientAuthenticationBasic.userName + ":" + httpClientAuthenticationBasic.password));
        httpClient.DefaultRequestHeaders.Add("Authorization", "Basic " + encoded);
        return this;
    }
    public async Task<HttpResponseMessage> SendAsync(
    string baseUrl,
    HttpMethod httpMethod,
    IContentBuilder contentBuilder = null,
    object body = null,
    IDictionary<string, string>? headers = null,
    CancellationToken cancellationToken = default) {
        if (contentBuilder == null)
            contentBuilder = new NoBodyContentBuilder();

        using var request = new HttpRequestBuilder()
            .WithUrl(baseUrl)
            .WithMethod(httpMethod)
            .WithBody(body)
            .WithContentBuilder(contentBuilder)
            .Build();

        // Applica gli header per-request (thread-safe)
        if (headers != null) {
            foreach (var kv in headers) {
                if (!string.IsNullOrEmpty(kv.Key) && kv.Value != null)
                    request.Headers.TryAddWithoutValidation(kv.Key, kv.Value);
            }
        }
        DateTime dtStartRequest = DateTime.UtcNow;
        if (rateLimiter != null) {
            using var lease = await rateLimiter.AcquireAsync(1);
            if (!lease.IsAcquired) {
                return new HttpResponseMessage(System.Net.HttpStatusCode.TooManyRequests) {
                    ReasonPhrase = "Rate limit exceeded",
                    Content = new StringContent("{\"error\":\"rate_limit_exceeded\"}", Encoding.UTF8, "application/json"),
                    RequestMessage = request
                };
            }
            if (request.Headers.Contains("X-RateLimit-TimeSpanElapsed"))
                request.Headers.Remove("X-RateLimit-TimeSpanElapsed");
            request.Headers.Add("X-RateLimit-TimeSpanElapsed", (DateTime.UtcNow - dtStartRequest).ToString());
        }

        if (_retryPolicy == null)
            return await _SendAsync(request, cancellationToken);

        return await _retryPolicy.ExecuteAsync(async ctx => {
            var attempt = ctx.ContainsKey("RetryAttempt") ? (int)ctx["RetryAttempt"] : 0;
            var backoff = ctx.ContainsKey("BackoffTime") ? (TimeSpan)ctx["BackoffTime"] : TimeSpan.Zero;
            if (request.Headers.Contains("X-Retry-Attempt")) request.Headers.Remove("X-Retry-Attempt");
            request.Headers.Add("X-Retry-Attempt", attempt.ToString());

            if (request.Headers.Contains("X-RateLimit-TimeSpanElapsed")) request.Headers.Remove("X-RateLimit-TimeSpanElapsed");
            request.Headers.Add("X-RateLimit-TimeSpanElapsed", backoff.ToString());

            using var clone = await CloneHttpRequestMessageAsync(request); // dispose del clone a fine tentativo
            return await _SendAsync(clone, cancellationToken);
        }, new Context());
    }
    private async Task<HttpResponseMessage> _SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) {
        var startedAt = DateTime.UtcNow;
        using var timeoutCts = new CancellationTokenSource(_timeout); // InfiniteTimeSpan = nessun timeout

        using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(
            timeoutCts.Token,
            cancellationToken
        );
        try {
            var response = await httpClient.SendAsync(request, linkedCts.Token);
            return response;
        } catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) {
            // Cancellazione del chiamante: si propaga (contratto .NET), non diventa una risposta 500
            throw;
        } catch (OperationCanceledException) {
            // Timer dell'helper (addTimeout) o HttpClient.Timeout di default → 408
            var elapsed = DateTime.UtcNow - startedAt;
            return new HttpResponseMessage(System.Net.HttpStatusCode.RequestTimeout) {
                ReasonPhrase = "Client timeout",
                Content = new StringContent(
                    $"{{\"error\":\"timeout\",\"elapsedSeconds\":{elapsed.TotalSeconds:F3}}}",
                    Encoding.UTF8,
                    "application/json"),
                RequestMessage = request
            };
        } catch (HttpRequestException ex) {
            return new HttpResponseMessage(System.Net.HttpStatusCode.BadGateway) {
                ReasonPhrase = "Upstream error",
                Content = new StringContent(
                    System.Text.Json.JsonSerializer.Serialize(new { error = "upstream", message = ex.Message }),
                    Encoding.UTF8,
                    "application/json"),
                RequestMessage = request
            };
        } catch (Exception ex) {
            return new HttpResponseMessage(System.Net.HttpStatusCode.InternalServerError) {
                ReasonPhrase = "Internal client error",
                Content = new StringContent(
                    System.Text.Json.JsonSerializer.Serialize(new { error = "internal_error", message = ex.Message }),
                    Encoding.UTF8,
                    "application/json"),
                RequestMessage = request
            };
        }
    }
    private static async Task<HttpRequestMessage> CloneHttpRequestMessageAsync(HttpRequestMessage request) {
        var clone = new HttpRequestMessage(request.Method, request.RequestUri);

        // Copia gli header
        foreach (var header in request.Headers) {
            clone.Headers.TryAddWithoutValidation(header.Key, header.Value);
        }

        // Clona il contenuto (se presente)
        if (request.Content != null) {
            // Nessun CancellationToken: il contenuto è già in memoria (String/ByteArray/FormUrlEncoded); la cancellazione resta gestita da _SendAsync
            var content = await request.Content.ReadAsByteArrayAsync();
            clone.Content = new ByteArrayContent(content);

            // Copia gli header del contenuto
            foreach (var header in request.Content.Headers) {
                clone.Content.Headers.TryAddWithoutValidation(header.Key, header.Value);
            }
        }
        return clone;
    }
    public IhttpsClientHelper addTimeout(TimeSpan timeSpan) {
        // Non tocca HttpClient.Timeout (immutabile dopo il primo invio e di proprietà del chiamante). Vale l'ultimo valore.
        _timeout = timeSpan;
        return this;
    }
    public IhttpsClientHelper addHeaders(string KeyName, string KeyValue) {
        httpClient.DefaultRequestHeaders.Add(KeyName, KeyValue);
        return this;
    }

    public IhttpsClientHelper ClearRequestActions() {
        _events.ClearAll();
        return this;
    }
}
public interface IhttpsClientHelper {
    /// <summary>
    /// central method for any http call
    /// </summary>
    /// <param name="baseUrl">Url to call</param>
    /// <param name="httpMethod">Get, Post...</param>
    /// <param name="contentBuilder">Type content</param>
    /// <param name="body">object to put</param>
    /// <param name="headers">Headers</param>
    /// <param name="cancellationToken">cancellationToken</param>
    /// <returns></returns>
    /// <exception cref="OperationCanceledException">Thrown when <paramref name="cancellationToken"/> is cancelled by the caller. Timeouts (<c>addTimeout</c> or <c>HttpClient.Timeout</c>) are returned as a 408 response instead.</exception>
    Task<HttpResponseMessage> SendAsync(
        string baseUrl,
        HttpMethod httpMethod,
        IContentBuilder contentBuilder = null, 
        object body = null,
        IDictionary<string, string>? headers = null,
        CancellationToken cancellationToken = default);
    IhttpsClientHelper AddRequestAction(Func<HttpRequestMessage, HttpResponseMessage, int, TimeSpan, Task> action);
    IhttpsClientHelper addFormData(List<KeyValuePair<string, string>> keyValuePairs);
    IhttpsClientHelper addRetryCondition(Func<HttpResponseMessage, bool> RetryCondition, int retryCount, double backoffFactor);
    IhttpsClientHelper addTimeout(TimeSpan timeSpan);
    IhttpsClientHelper ClearRequestActions();
    IhttpsClientHelper addHeaders(string KeyName, string KeyValue);
    IhttpsClientHelper setHeadersWithoutAuthorizationSync(Dictionary<string, string> _HeaderValues);
    IhttpsClientHelper setHeadersAndBearerAuthenticationSync(Dictionary<string, string> _HeaderValues, httpClientAuthenticationBearer httpClientAuthenticationBearer);
    IhttpsClientHelper setHeadersAndBasicAuthenticationSync(Dictionary<string, string> _HeaderValues, httpClientAuthenticationBasic httpClientAuthenticationBasic);
}