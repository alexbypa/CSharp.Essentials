using Castle.Core.Logging;
using CSharpEssentials.HttpHelper.HttpMocks;
using CSharpEssentials.LoggerHelper;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using System.Net;
using System.Net.Security;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;

namespace CSharpEssentials.HttpHelper;

public class HttpMockDelegatingHandler : DelegatingHandler {
    private readonly IHttpMockEngine? _engine;
    private HttpMessageHandler? _mockHandler;
    public HttpMockDelegatingHandler(IHttpMockEngine? engine = null) {
        _engine = engine;
    }
    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) {
        if (_engine == null)
            return await base.SendAsync(request, cancellationToken);

        if (!_engine.Match(request))
            return await base.SendAsync(request, cancellationToken);

        var _mockHandler = _engine.Build();
        var invoker = new HttpMessageInvoker(_mockHandler, disposeHandler: false);
        try {
            return await invoker.SendAsync(request, cancellationToken);
        } catch (MockException mex) {
            throw new InvalidOperationException(
                $"❌ Mock configuration error for request: {request.Method} {request.RequestUri}\n" +
                $"The mock matched the request but the setup is incomplete.\n" +
                $"Details: {mex.Message}",
                mex
            );
        } catch (Exception ex) {
            Console.WriteLine($"⚠️ Mock execution failed: {ex.Message}");
            return await base.SendAsync(request, cancellationToken);
        }
    }
}

#pragma warning disable SYSLIB0057
public static class CertificateConfigurator {
    public static void Apply(SocketsHttpHandler handler, httpClientOptions opt) {
        if (string.IsNullOrEmpty(opt?.Certificate?.Path) && string.IsNullOrEmpty(opt?.Certificate?.Password))
            return;
        if (!File.Exists(opt!.Certificate!.Path))
            loggerExtension<RequestHttpExtension>.TraceAsync(new RequestHttpExtension(), Serilog.Events.LogEventLevel.Fatal, new Exception("path certified error"), "Certificate file not found at path {path}", opt.Certificate.Path);
        try {
            var cert = new X509Certificate2(opt!.Certificate!.Path, opt!.Certificate!.Password);

            handler.SslOptions.EnabledSslProtocols = SslProtocols.Tls12 | SslProtocols.Tls13;
            handler.SslOptions.ClientCertificates = new X509CertificateCollection { cert };

            handler.SslOptions.RemoteCertificateValidationCallback = (sender, certificate, chain, sslPolicyErrors) => {
                loggerExtension<RequestHttpExtension>.TraceAsync(
                    new RequestHttpExtension(),
                    sslPolicyErrors == SslPolicyErrors.None ? Serilog.Events.LogEventLevel.Debug : Serilog.Events.LogEventLevel.Fatal,
                    null,
                    $"[{DateTime.Now:HH:mm:ss}] TLS handshake → server: {certificate?.Subject}, errori: {sslPolicyErrors}");
                return sslPolicyErrors == SslPolicyErrors.None;
            };
        } catch (Exception ex) {
            loggerExtension<RequestHttpExtension>.TraceAsync(new RequestHttpExtension(), Serilog.Events.LogEventLevel.Fatal, ex, "Error on load certificate with X509Certificate2");
        }
    }
}

public static class ProxyConfigurator {
    public static void Apply(SocketsHttpHandler handler, httpClientOptions opt) {
        if (opt.httpProxy == null || !opt.httpProxy.UseProxy)
            return;

        try {
        handler.Proxy = new WebProxy {
            Address = new Uri(opt.httpProxy.Address),
            Credentials = new NetworkCredential(opt.httpProxy.UserName, opt.httpProxy.Password)
        };
        }catch (Exception ex) {
            loggerExtension<RequestHttpExtension>.TraceAsync(
                new RequestHttpExtension(),
                Serilog.Events.LogEventLevel.Error,
                ex,
                "HttpHelper CONFIG : Proxy Error");
            return;
        }

        handler.UseProxy = true;

        loggerExtension<RequestHttpExtension>.TraceAsync(
            new RequestHttpExtension(),
            Serilog.Events.LogEventLevel.Warning,
            null,
            "HttpHelper CONFIG : ApplyProxy: UseProxy={UseProxy}, Address={Address}", opt.httpProxy?.UseProxy, opt.httpProxy?.Address);
    }
}

public static class httpExtension {
    public static IServiceCollection AddHttpClients(this IServiceCollection services, IConfiguration configuration) {
        var builder = new ConfigurationBuilder().AddConfiguration(configuration);
        Console.WriteLine($"[httpExtension] get builder");
        var externalConfigPath = Path.Combine(AppContext.BaseDirectory, "appsettings.httphelper.json");
        if (File.Exists(externalConfigPath)) {
            Console.WriteLine($"[httpExtension] ✅ TROVATO: {externalConfigPath}");
            builder.AddJsonFile(externalConfigPath, optional: false, reloadOnChange: true).AddEnvironmentVariables();
        }
        IConfiguration finalConfiguration = builder.Build();

        Console.WriteLine($"[httpExtension] configuration builded....");

        services.AddSingleton<IHttpRequestEvents, HttpRequestEvents>();
        Console.WriteLine($"[httpExtension] singleton IHttpRequestEvents....");
        services.AddTransient<HttpClientHandlerLogging>();
        Console.WriteLine($"[httpExtension] added HttpClientHandlerLogging ...");
        var httpclientoptions = finalConfiguration.GetSection("HttpClientOptions");
        services.Configure<List<httpClientOptions>>(httpclientoptions);
        List<httpClientOptions>? options = getOptions(httpclientoptions);
        Console.WriteLine($"[httpExtension] Loaded options ...");

        services.AddSingleton<IhttpsClientHelperFactory, httpsClientHelperFactory>();
        
        Console.WriteLine($"[httpExtension] Loaded httpsClientHelperFactory ...");

        services.InjectMock();

        Console.WriteLine($"[httpExtension] parsing appSettings.json...");
        if (options != null) {
            Console.WriteLine($"[httpExtension] founded appSettings.json !");
            foreach (var option in options) {
                Console.WriteLine($"[httpExtension] Trovato option: {option.Name}");

                services
                .AddHttpClient<IhttpsClientHelper, httpsClientHelper>(option.Name)
                .SetHandlerLifetime(TimeSpan.FromSeconds(30)) //TODO: sarebbe meglio metterlo su appSettings.json
                .AddHttpMessageHandler<HttpClientHandlerLogging>()
                .AddHttpMessageHandler<HttpMockDelegatingHandler>()
                .ConfigurePrimaryHttpMessageHandler(() => {
                    var handler = new SocketsHttpHandler();
                    if (option.UseCompression) {
                        handler.AutomaticDecompression =
                            DecompressionMethods.GZip | DecompressionMethods.Deflate;
                    }
                    CertificateConfigurator.Apply(handler, option);     //Certificate
                    ProxyConfigurator.Apply(handler, option);           //Proxy

                    return handler;
                });
            }
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