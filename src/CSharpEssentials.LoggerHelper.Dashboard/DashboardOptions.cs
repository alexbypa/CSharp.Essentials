using System.Security.Cryptography;
using System.Text;

namespace CSharpEssentials.LoggerHelper.Dashboard;

/// <summary>
/// Configuration for the LoggerHelper embedded dashboard.
/// </summary>
public sealed class DashboardOptions {
    private byte[]? _usernameHash;
    private byte[]? _passwordHash;

    /// <summary>
    /// URL path where the dashboard will be served. Default: "/loggerhelper".
    /// </summary>
    public string Path { get; set; } = "/loggerhelper";

    /// <summary>
    /// Optional name of an ASP.NET Core authorization policy required to access the dashboard.
    /// When combined with <see cref="UseBasicAuthentication"/> both must succeed (AND).
    /// </summary>
    public string? AuthorizationPolicy { get; set; }

    /// <summary>
    /// Auto-refresh interval in seconds. Default: 30.
    /// </summary>
    public int RefreshIntervalSeconds { get; set; } = 30;

    /// <summary>
    /// Protects the dashboard with HTTP Basic authentication using the given credentials.
    /// Only SHA-256 hashes are kept in memory. Use over HTTPS only.
    /// </summary>
    /// <exception cref="ArgumentException">Username or password is null, empty or whitespace.</exception>
    public void UseBasicAuthentication(string? username, string? password) {
        ArgumentException.ThrowIfNullOrWhiteSpace(username);
        ArgumentException.ThrowIfNullOrWhiteSpace(password);
        _usernameHash = SHA256.HashData(Encoding.UTF8.GetBytes(username));
        _passwordHash = SHA256.HashData(Encoding.UTF8.GetBytes(password));
    }

    internal bool UsesBasicAuthentication => _usernameHash is not null && _passwordHash is not null;
    internal byte[] UsernameHash => _usernameHash ?? [];
    internal byte[] PasswordHash => _passwordHash ?? [];
}
