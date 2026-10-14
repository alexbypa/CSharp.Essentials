using CSharpEssentials.LoggerHelper.Dashboard;
using CSharpEssentials.LoggerHelper.Diagnostics;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Serilog.Events;
using System.Net;
using System.Security.Claims;
using System.Text.Encodings.Web;

namespace CSharpEssentials.LoggerHelper.Tests;


/// <summary>Test authentication: header X-Test-User authenticates (X-Test-Role adds a role claim), otherwise NoResult.</summary>
internal sealed class TestAuthHandler : AuthenticationHandler<AuthenticationSchemeOptions> {
    public const string SchemeName = "TestAuth";

    public TestAuthHandler(IOptionsMonitor<AuthenticationSchemeOptions> options, ILoggerFactory logger, UrlEncoder encoder)
        : base(options, logger, encoder) { }

    protected override Task<AuthenticateResult> HandleAuthenticateAsync() {
        if (!Request.Headers.TryGetValue("X-Test-User", out var user))
            return Task.FromResult(AuthenticateResult.NoResult());
        var claims = new List<Claim> { new(ClaimTypes.Name, user.ToString()) };
        if (Request.Headers.TryGetValue("X-Test-Role", out var role))
            claims.Add(new Claim(ClaimTypes.Role, role.ToString()));
        var ticket = new AuthenticationTicket(new ClaimsPrincipal(new ClaimsIdentity(claims, SchemeName)), SchemeName);
        return Task.FromResult(AuthenticateResult.Success(ticket));
    }
}

/// <summary>In-memory dashboard host (TestServer), no static plugin registry involved.</summary>
internal sealed class DashboardTestHost : IAsyncDisposable {
    private readonly WebApplication _app;
    private readonly CancellationTokenSource _pump = new();
    private Task _pumpTask = Task.CompletedTask;

    public HttpClient Client { get; }

    private DashboardTestHost(WebApplication app) {
        _app = app;
        Client = app.GetTestClient();
    }

    public static readonly string[] Routes = ["/loggerhelper", "/loggerhelper/api/status", "/loggerhelper/api/logs", "/loggerhelper/api/stream"];

    public static WebApplication Build(
        Action<DashboardOptions>? configure,
        Action<IServiceCollection>? services = null,
        bool map = true) {
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();
        builder.Services.AddSingleton(new LoggerHelperOptions());
        builder.Services.AddSingleton<ILogErrorStore>(new LogErrorStore());
        builder.Services.AddSingleton<ILoadedSinkStore>(new LoadedSinkStore());
        builder.Services.AddSingleton(new ContextualLogBuffer());
        services?.Invoke(builder.Services);
        builder.Services.AddLoggerHelperDashboard(configure);
        var app = builder.Build();
        app.UseAuthentication();
        app.UseAuthorization();
        if (map)
            app.MapLoggerHelperDashboard();
        return app;
    }

    /// <summary>Host whose app default scheme is <see cref="TestAuthHandler.SchemeName"/>.</summary>
    public static async Task<DashboardTestHost> StartAsync(Action<DashboardOptions>? configure, Action<AuthorizationOptions>? authz = null) {
        var app = Build(configure, s => {
            s.AddAuthentication(TestAuthHandler.SchemeName)
                .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>(TestAuthHandler.SchemeName, null);
            if (authz is not null)
                s.AddAuthorization(authz);
        });
        await app.StartAsync();
        var host = new DashboardTestHost(app);
        host.StartPump();
        return host;
    }

    // The SSE endpoint only sends headers once new entries arrive.
    private void StartPump() {
        var buffer = _app.Services.GetRequiredService<ContextualLogBuffer>();
        _pumpTask = Task.Run(async () => {
            try {
                while (!_pump.IsCancellationRequested) {
                    buffer.Push(LogEventLevel.Information, "tick", "test", DateTime.UtcNow);
                    await Task.Delay(100, _pump.Token);
                }
            } catch (OperationCanceledException) { }
        });
    }

    public async Task<HttpResponseMessage> GetAsync(string route, Action<HttpRequestMessage>? configure = null) {
        using var request = new HttpRequestMessage(HttpMethod.Get, route);
        configure?.Invoke(request);
        return await Client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);
    }

    public async Task<HttpStatusCode> GetStatusAsync(string route, Action<HttpRequestMessage>? configure = null) {
        using var response = await GetAsync(route, configure);
        return response.StatusCode;
    }

    public async ValueTask DisposeAsync() {
        _pump.Cancel();
        try { await _pumpTask; } catch (OperationCanceledException) { }
        Client.Dispose();
        await _app.StopAsync();
        await _app.DisposeAsync();
        _pump.Dispose();
    }
}
