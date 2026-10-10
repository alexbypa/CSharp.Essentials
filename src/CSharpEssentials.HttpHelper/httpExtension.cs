using CSharpEssentials.HttpHelper.HttpMocks;
using CSharpEssentials.LoggerHelper;
using Serilog.Events;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
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
        if (_engine == null || !_engine.Match(request))
            return await base.SendAsync(request, cancellationToken);

        _mockHandler ??= _engine.Build();

        // A matched request never reaches the real network: exceptions thrown by a scenario (e.g. a simulated
        // HttpRequestException) propagate to the caller like a real transport failure.
        using var invoker = new HttpMessageInvoker(_mockHandler, disposeHandler: false);
        return await invoker.SendAsync(request, cancellationToken);
    }
}

#pragma warning disable SYSLIB0057
public static class CertificateConfigurator {
    public static void Apply(SocketsHttpHandler handler, httpClientOptions opt) {
        if (string.IsNullOrEmpty(opt?.Certificate?.Path) && string.IsNullOrEmpty(opt?.Certificate?.Password))
            return;
        if (string.IsNullOrEmpty(opt!.Certificate!.Path) || !File.Exists(opt.Certificate.Path)) {
            HttpHelperLog.Write(LogEventLevel.Fatal, new FileNotFoundException("Certificate file not found", opt.Certificate.Path), "Certificate file not found at path {path}", opt.Certificate.Path);
            return;
        }
        try {
            var cert = new X509Certificate2(opt!.Certificate!.Path, opt!.Certificate!.Password);

            handler.SslOptions.EnabledSslProtocols = SslProtocols.Tls12 | SslProtocols.Tls13;
            handler.SslOptions.ClientCertificates = new X509CertificateCollection { cert };

            handler.SslOptions.RemoteCertificateValidationCallback = (sender, certificate, chain, sslPolicyErrors) => {
                HttpHelperLog.Write(
                    sslPolicyErrors == SslPolicyErrors.None ? LogEventLevel.Debug : LogEventLevel.Fatal,
                    null,
                    "[{Time}] TLS handshake → server: {Subject}, errors: {Errors}",
                    DateTime.UtcNow.ToString("HH:mm:ss"), certificate?.Subject, sslPolicyErrors);
                return sslPolicyErrors == SslPolicyErrors.None;
            };
        } catch (Exception ex) {
            HttpHelperLog.Write(LogEventLevel.Fatal, ex, "Error on load certificate with X509Certificate2");
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
            HttpHelperLog.Write(LogEventLevel.Error, ex, "HttpHelper CONFIG : Proxy Error");
            return;
        }

        handler.UseProxy = true;

        HttpHelperLog.Write(LogEventLevel.Warning, null,
            "HttpHelper CONFIG : ApplyProxy: UseProxy={UseProxy}, Address={Address}", opt.httpProxy?.UseProxy, opt.httpProxy?.Address);
    }
}

public static class httpExtension {
    public static IServiceCollection AddHttpClients(this IServiceCollection services, IConfiguration configuration) {
        var httpclientoptions = configuration.GetSection("HttpClientOptions");
        services.Configure<List<httpClientOptions>>(httpclientoptions);
        services.Configure<HttpHelperLoggingOptions>(configuration.GetSection("HttpHelperLogging"));
        List<httpClientOptions>? options = getOptions(httpclientoptions);

        // IHttpClientFactory must be registered even when no client is configured (CreateOrGet then throws a clear ArgumentException).
        services.AddHttpClient();
        services.TryAddSingleton<IHttpRequestEvents, HttpRequestEvents>();
        services.TryAddSingleton<HttpRequestEventsRegistry>();
        services.AddTransient<HttpClientHandlerLogging>();
        services.TryAddSingleton<IhttpsClientHelperFactory, httpsClientHelperFactory>();

        services.InjectMock();

        if (options != null) {
            foreach (var option in options) {
                var name = option.Name;
                // Named clients only: helpers are created (and cached) by IhttpsClientHelperFactory.
                services
                .AddHttpClient(name)
                .SetHandlerLifetime(TimeSpan.FromSeconds(30)) //TODO: sarebbe meglio metterlo su appSettings.json
                // Per-client events (callbacks of one client never fire for another) + global events.
                .AddHttpMessageHandler(sp => new HttpClientHandlerLogging(
                    sp.GetRequiredService<HttpRequestEventsRegistry>().For(name),
                    sp.GetRequiredService<IHttpRequestEvents>(),
                    name,
                    sp.GetRequiredService<IOptionsMonitor<HttpHelperLoggingOptions>>()))
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
    public string IdTransaction => Guid.NewGuid().ToString("N");

    public string Action => "HttpHelper";

    public string ApplicationName => "HttpHelper";
}