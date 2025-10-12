using Microsoft.AspNetCore.Http;

namespace CSharpEssentials.LoggerHelper.Dashboard.Extensions;
public sealed class DashboardContext {
    public HttpContext HttpContext { get; }
    public DashboardContext(HttpContext httpContext) => HttpContext = httpContext;
}

public interface IDashboardAuthorizationFilter {
    bool Authorize(DashboardContext context);
}

public sealed class DashboardOptions {
    public IList<IDashboardAuthorizationFilter> Authorization { get; } = new List<IDashboardAuthorizationFilter>();
    public string Realm { get; set; } = "LoggerHelper Dashboard";
}

// Basic Auth filter stile Hangfire.BasicAuthorization
public sealed class BasicAuthAuthorizationFilter : IDashboardAuthorizationFilter {
    public sealed record User(string Login, string Password);

    private readonly IReadOnlyList<User> _users;
    private readonly bool _requireSsl;
    private readonly bool _loginCaseSensitive;
    private readonly string _realm;

    public BasicAuthAuthorizationFilter(
        IEnumerable<User> users,
        bool requireSsl = false,
        bool loginCaseSensitive = false,
        string? realm = null) {
        _users = users.ToList();
        _requireSsl = requireSsl;
        _loginCaseSensitive = loginCaseSensitive;
        _realm = realm ?? "LoggerHelper Dashboard";
    }

    public bool Authorize(DashboardContext context) {
        var http = context.HttpContext;

        if (_requireSsl && !http.Request.IsHttps) {
            http.Response.StatusCode = StatusCodes.Status403Forbidden;
            return false;
        }

        if (!http.Request.Headers.TryGetValue("Authorization", out var header) ||
            !header.ToString().StartsWith("Basic ", StringComparison.OrdinalIgnoreCase)) {
            Challenge(http);
            return false;
        }

        var token = header.ToString().Substring("Basic ".Length).Trim();
        string decoded;
        try {
            decoded = System.Text.Encoding.UTF8.GetString(Convert.FromBase64String(token));
        } catch {
            Challenge(http);
            return false;
        }

        var parts = decoded.Split(':', 2);
        if (parts.Length != 2) { Challenge(http); return false; }

        var login = parts[0];
        var pwd = parts[1];

        foreach (var u in _users) {
            var loginMatch = _loginCaseSensitive
                ? string.Equals(u.Login, login, StringComparison.Ordinal)
                : string.Equals(u.Login, login, StringComparison.OrdinalIgnoreCase);

            if (loginMatch && SecureEquals(u.Password, pwd))
                return true;
        }

        Challenge(context.HttpContext);
        return false;
    }

    private void Challenge(HttpContext http) {
        http.Response.Headers["WWW-Authenticate"] = $"Basic realm=\"{_realm}\"";
        http.Response.StatusCode = StatusCodes.Status401Unauthorized;
    }

    // confronto temporizzato
    private static bool SecureEquals(string a, string b) {
        var x = System.Text.Encoding.UTF8.GetBytes(a);
        var y = System.Text.Encoding.UTF8.GetBytes(b);
        if (x.Length != y.Length)
            return false;
        var diff = 0;
        for (int i = 0; i < x.Length; i++)
            diff |= x[i] ^ y[i];
        return diff == 0;
    }
}