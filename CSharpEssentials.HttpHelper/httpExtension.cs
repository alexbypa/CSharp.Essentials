using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace CSharpEssentials.HttpHelper;
public static class httpExtension {
    public static IServiceCollection AddHttpClients(this IServiceCollection services, IConfiguration configuration) {
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
        if (options != null)
            foreach (var option in options) {
                services
                    .AddHttpClient<IhttpsClientHelper, httpsClientHelper>(option.Name)
                    .SetHandlerLifetime(TimeSpan.FromSeconds(30))
                    .AddHttpMessageHandler<HttpClientHandlerLogging>();

                //services.AddHttpClient(option.Name)
                //    .SetHandlerLifetime(TimeSpan.FromSeconds(30))
                //    .AddHttpMessageHandler<HttpClientHandlerLogging>();

                //services.AddScoped<IhttpsClientHelper>(sp => {
                //    var factory = sp.GetRequiredService<IHttpClientFactory>();
                //    return new httpsClientHelper(factory, option.Name);
                //});
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
