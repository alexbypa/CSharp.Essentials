using Microsoft.Extensions.Options;
using System.Collections.Concurrent;

namespace CSharpEssentials.HttpHelper;
public class httpsClientHelperFactory : IhttpsClientHelperFactory {
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IEnumerable<httpClientOptions> _options;
    private readonly IHttpRequestEvents _events;
    private readonly HttpRequestEventsRegistry _registry;
    // Lazy: GetOrAdd may run the value factory more than once under contention; Lazy guarantees a single helper (and HttpClient/RateLimiter) per name.
    private readonly ConcurrentDictionary<string, Lazy<IhttpsClientHelper>> _cache = new();
    private volatile IhttpsClientHelper? _lastCreated;

    public httpsClientHelperFactory(
        IHttpClientFactory httpClientFactory,
        IHttpRequestEvents events,
        IOptions<List<httpClientOptions>> options)
        : this(httpClientFactory, events, options, new HttpRequestEventsRegistry()) {
    }
    public httpsClientHelperFactory(
        IHttpClientFactory httpClientFactory,
        IHttpRequestEvents events,
        IOptions<List<httpClientOptions>> options,
        HttpRequestEventsRegistry registry) {
        _httpClientFactory = httpClientFactory;
        _options = options.Value;
        _events = events;
        _registry = registry;
    }

    public IhttpsClientHelper CreateOrGet(string name) {
        // Validate before touching the cache: unknown names must not be cached.
        var config = _options.FirstOrDefault(o => o.Name == name)
            ?? throw new ArgumentException($"Client '{name}' not found");

        var lazy = _cache.GetOrAdd(name, n => new Lazy<IhttpsClientHelper>(() => {
            // Client creato qui e usato solo da questo helper: la factory può azzerarne il timeout, così addTimeout
            // non viene limitato da HttpClient.Timeout; il valore configurato resta il default del helper.
            var client = _httpClientFactory.CreateClient(n);
            var defaultTimeout = client.Timeout;
            client.Timeout = Timeout.InfiniteTimeSpan;
            return new httpsClientHelper(client, _registry.For(n), config.RateLimitOptions).addTimeout(defaultTimeout);
        }));
        try {
            var helper = lazy.Value;
            _lastCreated = helper;
            return helper;
        } catch {
            _cache.TryRemove(new KeyValuePair<string, Lazy<IhttpsClientHelper>>(name, lazy)); // Lazy caches exceptions: don't keep a broken entry
            throw;
        }
    }
    public IhttpsClientHelper AddActionOnRequest(Func<HttpRequestMessage, HttpResponseMessage, int, TimeSpan, Task> callback) {
        _events.Add(callback);
        return _lastCreated!;
    }
}
public interface IhttpsClientHelperFactory {
    IhttpsClientHelper CreateOrGet(string name);
    /// <summary>Registers <paramref name="callback"/> for the requests of the client <paramref name="name"/> only.</summary>
    IhttpsClientHelper AddActionOnRequest(string name, Func<HttpRequestMessage, HttpResponseMessage, int, TimeSpan, Task> callback) {
        var helper = CreateOrGet(name);
        helper.AddRequestAction(callback);
        return helper;
    }
    /// <summary>
    /// Registers <paramref name="callback"/> for the requests of <b>all</b> clients, regardless of call order.
    /// Returns the last helper returned by <see cref="CreateOrGet"/>, or <c>null</c> if none was created yet.
    /// </summary>
    IhttpsClientHelper AddActionOnRequest(Func<HttpRequestMessage, HttpResponseMessage, int, TimeSpan, Task> callback);
}
