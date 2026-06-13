namespace CSharpEssentials.LoggerHelper;

/// <summary>
/// Configuration for the built-in sensitive data masking enricher.
/// Maps to JSON section "LoggerHelper:SensitiveDataMasking".
/// Also configurable via <see cref="LoggerHelperBuilder.EnableSensitiveDataMasking"/>.
/// </summary>
public sealed class SensitiveDataMaskingOptions {
    /// <summary>
    /// Master switch. When false (default), no masking enricher is added to the pipeline
    /// and there is zero runtime overhead.
    /// </summary>
    public bool Enabled { get; set; }

    /// <summary>
    /// Text used to replace masked content. Default: "***MASKED***".
    /// </summary>
    public string MaskText { get; set; } = "***MASKED***";

    /// <summary>
    /// Named built-in pattern presets to enable. Recognized values (case-insensitive):
    /// "Email", "CreditCard", "JwtToken", "BearerToken", "ConnectionStringSecret".
    /// </summary>
    public List<string> Presets { get; set; } = [];

    /// <summary>
    /// Additional custom regex rules, evaluated against every scalar string property value
    /// (and the rendered message, when <c>EnableRenderedMessage</c> is on).
    /// Use a named capture group called "secret" to mask only part of the match.
    /// </summary>
    public List<MaskingRule> Rules { get; set; } = [];

    /// <summary>
    /// Structured property names (case-insensitive) whose value is replaced entirely with
    /// <see cref="MaskText"/>, regardless of content — e.g. "Password", "Ssn", "ApiKey".
    /// </summary>
    public List<string> SensitiveProperties { get; set; } = [];
}

/// <summary>
/// A single custom masking rule: a regular expression with an optional name for diagnostics.
/// </summary>
public sealed class MaskingRule {
    /// <summary>
    /// Diagnostic name for the rule (not used for matching).
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Regular expression pattern. If it contains a named group "secret",
    /// only that group is replaced; otherwise the entire match is replaced.
    /// </summary>
    public string Pattern { get; set; } = string.Empty;
}
