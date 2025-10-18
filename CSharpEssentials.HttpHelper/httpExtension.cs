using CSharpEssentials.LoggerHelper;
using CSharpEssentials.LoggerHelper.model;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;

namespace CSharpEssentials.HttpHelper;
public static class httpExtension {
    private static HttpMessageHandler checkForMock(string primaryHandlerMethodName) {
        HttpMessageHandler primaryHandlerInstance = null;
        if (!string.IsNullOrEmpty(primaryHandlerMethodName)) {
            try {
                Type? handlerType = AppDomain.CurrentDomain.GetAssemblies()
                                        .SelectMany(a => a.GetTypes())
                                        .FirstOrDefault(t => t.Name == primaryHandlerMethodName.Split(".").FirstOrDefault()); // Cerca la classe di Mock

                if (handlerType != null) {
                    MethodInfo? createHandlerMethod = handlerType.GetMethod(
                        primaryHandlerMethodName.Split(".").Last(),
                        BindingFlags.Public | BindingFlags.Static
                    );
                    if (createHandlerMethod != null && createHandlerMethod.ReturnType == typeof(HttpMessageHandler)) {
                        primaryHandlerInstance = createHandlerMethod.Invoke(null, null) as HttpMessageHandler;
                    }
                }
                if (primaryHandlerInstance == null) {
                    GlobalLogger.Errors.Add(new LogErrorEntry {
                        ContextInfo = "HttpHelper",
                        ErrorMessage = $"Mock {primaryHandlerMethodName} not founded",
                        SinkName = "HttpHelper",
                        Timestamp = DateTime.Now,
                        StackTrace = "No static class Moq founded , the method must be static !"
                    });
                }
            } catch (Exception ex) {
                GlobalLogger.Errors.Add(new LogErrorEntry {
                    ContextInfo = "HttpHelper",
                    ErrorMessage = $"Error on mock {primaryHandlerMethodName} : {ex.Message }",
                    SinkName = "HttpHelper",
                    Timestamp = DateTime.Now,
                    StackTrace = ex.StackTrace
                });
            }
        }
        return primaryHandlerInstance;
    }
    public static IServiceCollection AddHttpClients(this IServiceCollection services, IConfiguration configuration, SocketsHttpHandler socketsHttpHandler = null) {
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
                    .ConfigurePrimaryHttpMessageHandler(() => checkForMock(option.Mock) ?? socketsHttpHandler ?? new SocketsHttpHandler());
                //.ConfigurePrimaryHttpMessageHandler(() => PrimaryHandler ?? new SocketsHttpHandler());

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
