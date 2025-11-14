using CSharpEssentials.LoggerHelper;
using CSharpEssentials.LoggerHelper.model;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System.Net;
using System.Net.Security;
using System.Reflection;
using System.Security.Cryptography.X509Certificates;

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
                    ErrorMessage = $"Error on mock {primaryHandlerMethodName} : {ex.Message}",
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
            configurationBuilder.AddJsonFile(externalConfigPath, optional: true, reloadOnChange: true).AddEnvironmentVariables();
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
                    .ConfigurePrimaryHttpMessageHandler(() => {
                        HttpMessageHandler handler = checkForMock(option.Mock) ?? socketsHttpHandler ?? new SocketsHttpHandler();
                        if (option.Certificate != null && !string.IsNullOrEmpty(option.Certificate.Path) && !string.IsNullOrEmpty(option.Certificate!.Password)) {
                            Console.WriteLine($"Loading certificate from path: {option.Certificate.Path}");
                            loggerExtension<RequestHttpExtension>.TraceAsync(new RequestHttpExtension(), Serilog.Events.LogEventLevel.Debug, null, "Loading certificate from path: {Path}", option.Certificate.Path);
                            if (!File.Exists(option.Certificate.Path)) {
                                loggerExtension<RequestHttpExtension>.TraceAsync(new RequestHttpExtension(), Serilog.Events.LogEventLevel.Fatal, new Exception("path certified error"), "Certificate file not found at path {path}", option.Certificate.Path);
                                throw new FileNotFoundException($"Certificate file not found at path: {option.Certificate.Path}");
                            }

                            var clientCertificate = new X509Certificate2(
                                option.Certificate.Path,
                                option.Certificate.Password
                            );

                            if (handler is SocketsHttpHandler socketsHandler) {
                                loggerExtension<RequestHttpExtension>.TraceAsync(new RequestHttpExtension(), Serilog.Events.LogEventLevel.Debug, null, "Abilitato ptotocollo SSL Tls12 e Tls13: {Path}", option.Certificate.Path);
                                socketsHandler.SslOptions.EnabledSslProtocols = System.Security.Authentication.SslProtocols.Tls12 | System.Security.Authentication.SslProtocols.Tls13;

                                //socketsHandler.SslOptions.client = 
                                socketsHandler.SslOptions.RemoteCertificateValidationCallback = (sender, certificate, chain, sslPolicyErrors) => {
                                    loggerExtension<RequestHttpExtension>.TraceAsync(new RequestHttpExtension(), Serilog.Events.LogEventLevel.Debug, null, $"[{DateTime.Now:HH:mm:ss}] TLS handshake → server: {certificate?.Subject}, errori: {sslPolicyErrors}");
                                    return sslPolicyErrors == SslPolicyErrors.None;
                                };
                                socketsHandler.SslOptions.ClientCertificates = [clientCertificate];
                            }
                        }

                        //Gestione Proxy
                        if (handler is SocketsHttpHandler httpClienthandler) {
                            if (option.httpProxy != null && option.httpProxy.UseProxy && option.httpProxy.UseProxy) {
                                httpClienthandler.Proxy = new WebProxy {
                                    Address = new Uri(option.httpProxy.Address),
                                    Credentials = new NetworkCredential(option.httpProxy.UserName, option.httpProxy.Password)
                                };
                                httpClienthandler.UseProxy = option.httpProxy.UseProxy;
                            }
                        }
                        return handler;
                    });

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

public class RequestHttpExtension : IRequest {
    public string IdTransaction => DateTime.Now.ToString();

    public string Action => "HttpHelper";

    public string ApplicationName => "HttpHelper";
}