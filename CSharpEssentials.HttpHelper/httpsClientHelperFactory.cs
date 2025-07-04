using Microsoft.Extensions.Options;

namespace CSharpEssentials.HttpHelper;
public class httpsClientHelperFactory : IhttpsClientHelperFactory {
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IEnumerable<httpClientOptions> _options;
    private readonly IHttpRequestEvents _events;
    private readonly Dictionary<string, IhttpsClientHelper> _cache = new();
    private IhttpsClientHelper instance;
    public httpsClientHelperFactory(
        IHttpClientFactory httpClientFactory,
        IHttpRequestEvents events,
        IOptions<List<httpClientOptions>> options) {
        _httpClientFactory = httpClientFactory;
        _options = options.Value;
        _events = events;
    }

    public IhttpsClientHelper CreateOrGet(string name) {
        if (_cache.TryGetValue(name, out var cachedHelper))
            return cachedHelper;

        var config = _options.FirstOrDefault(o => o.Name == name)
        ?? throw new ArgumentException($"Client '{name}' not found");

        var client = _httpClientFactory.CreateClient(name);
        instance = new httpsClientHelper(client, _events, config.RateLimitOptions); // sostituisci `HttpRequestEvents` se già registrato
        _cache[name] = instance;

        return instance;
    }
    public IhttpsClientHelper AddActionOnRequest(Func<HttpRequestMessage, HttpResponseMessage, int, TimeSpan, Task> callback) {
        instance.AddRequestAction(callback);
        return instance;
    }
}
public interface IhttpsClientHelperFactory {
    IhttpsClientHelper CreateOrGet(string name);
    IhttpsClientHelper AddActionOnRequest(Func<HttpRequestMessage, HttpResponseMessage, int, TimeSpan, Task> callback);
}