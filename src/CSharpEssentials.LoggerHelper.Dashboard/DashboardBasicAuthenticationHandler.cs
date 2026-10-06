using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Net.Http.Headers;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Encodings.Web;

namespace CSharpEssentials.LoggerHelper.Dashboard;

/// <summary>
/// HTTP Basic authentication handler for the dashboard (credentials from <see cref="DashboardOptions"/>).
/// </summary>
internal sealed class DashboardBasicAuthenticationHandler : AuthenticationHandler<AuthenticationSchemeOptions> {
    internal const string SchemeName = "LoggerHelperDashboardBasic";

    private readonly DashboardOptions _dashboardOptions;

    public DashboardBasicAuthenticationHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder,
        DashboardOptions dashboardOptions) : base(options, logger, encoder) {
        _dashboardOptions = dashboardOptions;
    }

    protected override Task<AuthenticateResult> HandleAuthenticateAsync() {
        if (!Request.Headers.TryGetValue(HeaderNames.Authorization, out var header) || header.Count == 0)
            return Task.FromResult(AuthenticateResult.NoResult());

        if (!AuthenticationHeaderValue.TryParse(header.ToString(), out var parsed)
            || !string.Equals(parsed.Scheme, "Basic", StringComparison.OrdinalIgnoreCase)
            || string.IsNullOrEmpty(parsed.Parameter))
            return Task.FromResult(AuthenticateResult.NoResult());

        var buffer = new byte[parsed.Parameter.Length];
        if (!Convert.TryFromBase64String(parsed.Parameter, buffer, out var written))
            return Task.FromResult(AuthenticateResult.Fail("Invalid Basic credentials"));

        var decoded = Encoding.UTF8.GetString(buffer, 0, written);
        var sep = decoded.IndexOf(':');
        if (sep < 0)
            return Task.FromResult(AuthenticateResult.Fail("Invalid Basic credentials"));

        var user = decoded[..sep];
        var pass = decoded[(sep + 1)..];

        var userOk = CryptographicOperations.FixedTimeEquals(SHA256.HashData(Encoding.UTF8.GetBytes(user)), _dashboardOptions.UsernameHash);
        var passOk = CryptographicOperations.FixedTimeEquals(SHA256.HashData(Encoding.UTF8.GetBytes(pass)), _dashboardOptions.PasswordHash);
        // Non-short-circuit: both comparisons always evaluated.
        if (!(userOk & passOk))
            return Task.FromResult(AuthenticateResult.Fail("Invalid Basic credentials"));

        var identity = new ClaimsIdentity([new Claim(ClaimTypes.Name, user)], Scheme.Name);
        var ticket = new AuthenticationTicket(new ClaimsPrincipal(identity), Scheme.Name);
        return Task.FromResult(AuthenticateResult.Success(ticket));
    }

    protected override Task HandleChallengeAsync(AuthenticationProperties properties) {
        Response.StatusCode = StatusCodes.Status401Unauthorized;
        Response.Headers.WWWAuthenticate = "Basic realm=\"LoggerHelper Dashboard\", charset=\"UTF-8\"";
        return Task.CompletedTask;
    }
}
