namespace CSharpEssentials.LoggerHelper;

/// <summary>
/// Interface representing the basic logging request data.
/// Used by the legacy loggerExtension&lt;T&gt; API for backward compatibility.
/// New code should use ILogger&lt;T&gt; with BeginTrace/Trace extensions instead.
/// </summary>
public interface ILoggerRequest {
    /// <summary>
    /// Gets the unique transaction ID for tracking logs.
    /// </summary>
    string IdTransaction { get; }

    /// <summary>
    /// Gets the action name or context for the log entry.
    /// </summary>
    string Action { get; }

    /// <summary>
    /// Gets the application name associated with the log entry.
    /// </summary>
    string ApplicationName { get; }
}

/// <summary>
/// Marker interface extending ILoggerRequest for request-based logging.
/// Used by the legacy loggerExtension&lt;T&gt; API for backward compatibility.
/// New code should use ILogger&lt;T&gt; with BeginTrace/Trace extensions instead.
/// </summary>
public interface IRequest : ILoggerRequest { }
