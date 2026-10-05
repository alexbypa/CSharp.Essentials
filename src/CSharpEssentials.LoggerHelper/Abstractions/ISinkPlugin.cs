using Serilog;

namespace CSharpEssentials.LoggerHelper;

/// <summary>
/// Contract that every sink plugin must implement.
/// Each plugin knows how to handle one or more sink types
/// and configures the Serilog pipeline accordingly.
/// </summary>
public interface ISinkPlugin {
    /// <summary>
    /// Returns true if this plugin can handle the given sink name.
    /// </summary>
    bool CanHandle(string sinkName);

    /// <summary>
    /// Configures the sink on the Serilog LoggerConfiguration,
    /// using the routing condition and global options.
    /// Implementations must throw <see cref="InvalidOperationException"/> when required configuration is
    /// missing or invalid; the routing engine records the failure (error store, SelfLog) and marks the sink as not configured.
    /// </summary>
    void Configure(LoggerConfiguration loggerConfig, SinkRouting routing, LoggerHelperOptions options);
}
