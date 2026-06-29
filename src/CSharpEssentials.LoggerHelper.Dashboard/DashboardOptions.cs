namespace CSharpEssentials.LoggerHelper.Dashboard;

/// <summary>
/// Configuration for the LoggerHelper embedded dashboard.
/// </summary>
public sealed class DashboardOptions {
    /// <summary>
    /// URL path where the dashboard will be served. Default: "/loggerhelper".
    /// </summary>
    public string Path { get; set; } = "/loggerhelper";

    /// <summary>
    /// Require ASP.NET Core authorization to access the dashboard.
    /// </summary>
    public bool RequireAuthorization { get; set; }

    /// <summary>
    /// Auto-refresh interval in seconds. Default: 30.
    /// </summary>
    public int RefreshIntervalSeconds { get; set; } = 30;
}
