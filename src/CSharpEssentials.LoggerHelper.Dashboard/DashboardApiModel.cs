namespace CSharpEssentials.LoggerHelper.Dashboard;

public sealed class DashboardData {
    public string ApplicationName { get; init; } = string.Empty;
    public string Status { get; init; } = "OK";
    public string Uptime { get; init; } = string.Empty;
    public int ActiveSinks { get; init; }
    public int FailedSinks { get; init; }
    public int TotalRoutes { get; init; }
    public int ErrorCount { get; init; }
    public bool MaskingEnabled { get; init; }
    public List<DashboardSink> Sinks { get; init; } = [];
    public List<DashboardError> Errors { get; init; } = [];
    public List<DashboardRoute> Routes { get; init; } = [];
}

public sealed class DashboardSink {
    public string Name { get; init; } = string.Empty;
    public string PluginType { get; init; } = string.Empty;
    public List<string> Levels { get; init; } = [];
    public bool Active { get; init; }
}

public sealed class DashboardError {
    public string Timestamp { get; init; } = string.Empty;
    public string SinkName { get; init; } = string.Empty;
    public string Message { get; init; } = string.Empty;
    public string? StackTrace { get; init; }
    public string? Context { get; init; }
}

public sealed class DashboardRoute {
    public string Sink { get; init; } = string.Empty;
    public List<string> Levels { get; init; } = [];
}
