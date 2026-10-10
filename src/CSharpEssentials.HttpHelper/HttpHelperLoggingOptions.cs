namespace CSharpEssentials.HttpHelper;

/// <summary>Binds the <c>HttpHelperLogging</c> configuration section: opt-in logging of outgoing HTTP requests.</summary>
public sealed class HttpHelperLoggingOptions {
    /// <summary>
    /// Named clients whose requests are logged. <c>"*"</c> = every named client; names are compared
    /// ordinal to <c>httpClientOptions.Name</c>. Empty or missing = logging off.
    /// </summary>
    public List<string> LogRequests { get; set; } = [];

    /// <summary>
    /// Request header carrying a correlation id (e.g. <c>"X-Correlation-ID"</c>). Null or empty = off.
    /// A value already present on the request is never overwritten; otherwise the current W3C trace id
    /// (or a new GUID) is added and reused across retries. Also logged as <c>CorrelationId</c> when logging is on.
    /// </summary>
    public string? CorrelationIdHeader { get; set; }
}
