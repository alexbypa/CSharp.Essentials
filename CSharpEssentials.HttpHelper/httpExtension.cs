using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;

namespace CSharpEssentials.HttpHelper;
public static class httpExtension {
    public static IServiceCollection AddHttpClients(this IServiceCollection services, IConfiguration configuration, HttpMessageHandler PrimaryHandler) {
        var configurationBuilder = new ConfigurationBuilder().AddConfiguration(configuration);  // Usa la configurazione di partenza

        var externalConfigPath = Path.Combine(Directory.GetCurrentDirectory(), "appsettings.httpHelper.json");
        if (File.Exists(externalConfigPath)) {
            configurationBuilder.AddJsonFile(externalConfigPath, optional: true, reloadOnChange: true);
        }
        IConfiguration finalConfiguration = configurationBuilder.Build();

        services.AddSingleton<IHttpRequestEvents, HttpRequestEvents>();
        services.AddTransient<HttpClientHandlerLogging>();


        var httpclientoptions = finalConfiguration.GetSection("HttpClientOptions");

        services.Configure<List<httpClientOptions>>(httpclientoptions);
        List<httpClientOptions>? options = getOptions(httpclientoptions);
        
        services.AddSingleton<IhttpsClientHelperFactory, httpsClientHelperFactory>();

        Assembly assemblyCorrente = Assembly.GetExecutingAssembly();


        if (options != null)
            foreach (var option in options) {
                services
                    .AddHttpClient<IhttpsClientHelper, httpsClientHelper>(option.Name)
                    .SetHandlerLifetime(TimeSpan.FromSeconds(30))
                    .AddHttpMessageHandler<HttpClientHandlerLogging>()
                    .ConfigurePrimaryHttpMessageHandler(() => PrimaryHandler ?? new SocketsHttpHandler());

                services.AddSingleton<IhttpsClientHelper>(sp => {
                    var factory = sp.GetRequiredService<IHttpClientFactory>();
                    var client = factory.CreateClient(option.Name);
                    return new httpsClientHelper(client, sp.GetRequiredService<IHttpRequestEvents>(), option.RateLimitOptions);
                });
            }

        return services;
    }
    private static List<httpClientOptions>? getOptions(IConfigurationSection httpclientoptions) {
        if (httpclientoptions == null)
            return null;
        else
            return httpclientoptions.Get<List<httpClientOptions>>();
    }
}
