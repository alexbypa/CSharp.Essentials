using Microsoft.Extensions.Options;

namespace CSharpEssentials.HttpHelper;
public class httpsClientHelperFactory : IhttpsClientHelperFactory {
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IEnumerable<httpClientOptions> _options;
    private readonly IHttpRequestEvents _events;
    private readonly Dictionary<string, IhttpsClientHelper> _cache = new();

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
        var helper = new httpsClientHelper(client, new HttpRequestEvents(), config.RateLimitOptions); // sostituisci `HttpRequestEvents` se già registrato
        _cache[name] = helper;

        return helper;
    }
}

public interface IhttpsClientHelperFactory {
    IhttpsClientHelper CreateOrGet(string name);
}