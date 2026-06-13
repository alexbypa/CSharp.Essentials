using Serilog.Core;
using Serilog.Events;
using System.Text.RegularExpressions;

namespace CSharpEssentials.LoggerHelper;

/// <summary>
/// Enricher that redacts PII and secrets from structured log properties (and the rendered
/// message, when present) before they reach any sink — Console, File, Elasticsearch, SQL, etc.
///
/// Driven entirely by <see cref="SensitiveDataMaskingOptions"/>:
///   - <see cref="SensitiveDataMaskingOptions.SensitiveProperties"/> replaces a property's
///     value outright, regardless of content (e.g. "Password", "ApiKey").
///   - <see cref="SensitiveDataMaskingOptions.Presets"/> and
///     <see cref="SensitiveDataMaskingOptions.Rules"/> run regex substitutions against every
///     scalar string property. A named capture group called "secret" masks only that part
///     of the match; otherwise the whole match is replaced.
///
/// One JSON block protects every sink at once — no per-sink configuration, no code changes
/// at call sites.
/// </summary>
public sealed class SensitiveDataMaskingEnricher : ILogEventEnricher {
    private const string SecretGroupName = "secret";

    private readonly SensitiveDataMaskingOptions _options;
    private readonly Regex[] _patterns;
    private readonly HashSet<string> _sensitiveProperties;

    /// <summary>
    /// Built-in regex presets, keyed by case-insensitive name.
    /// Patterns using the "secret" named group mask only that portion of the match.
    /// </summary>
    public static readonly IReadOnlyDictionary<string, string> BuiltInPresetPatterns =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase) {
            ["Email"] = @"\b[A-Za-z0-9._%+-]+@[A-Za-z0-9.-]+\.[A-Za-z]{2,}\b",
            ["CreditCard"] = @"\b(?:\d[ -]?){13,19}\b",
            ["JwtToken"] = @"\beyJ[A-Za-z0-9_-]+\.[A-Za-z0-9_-]+\.[A-Za-z0-9_-]*\b",
            ["BearerToken"] = @"(?i)\bBearer\s+(?<secret>[A-Za-z0-9._-]+)",
            ["ConnectionStringSecret"] = @"(?i)\b(?:password|pwd)\s*=\s*(?<secret>[^;]*)",
        };

    public SensitiveDataMaskingEnricher(SensitiveDataMaskingOptions options) {
        _options = options;
        _sensitiveProperties = new HashSet<string>(options.SensitiveProperties, StringComparer.OrdinalIgnoreCase);
        _patterns = BuildPatterns(options);
    }

    private static Regex[] BuildPatterns(SensitiveDataMaskingOptions options) {
        var patterns = new List<Regex>();

        foreach (var preset in options.Presets) {
            if (BuiltInPresetPatterns.TryGetValue(preset, out var pattern))
                patterns.Add(new Regex(pattern, RegexOptions.Compiled));
        }

        foreach (var rule in options.Rules) {
            if (!string.IsNullOrWhiteSpace(rule.Pattern))
                patterns.Add(new Regex(rule.Pattern, RegexOptions.Compiled));
        }

        return patterns.ToArray();
    }

    public void Enrich(LogEvent logEvent, ILogEventPropertyFactory propertyFactory) {
        if (_patterns.Length == 0 && _sensitiveProperties.Count == 0)
            return;

        foreach (var name in logEvent.Properties.Keys.ToArray()) {
            // Handled separately below, after the rest of the properties are masked.
            if (name == "RenderedMessage")
                continue;

            if (_sensitiveProperties.Contains(name)) {
                logEvent.AddOrUpdateProperty(propertyFactory.CreateProperty(name, _options.MaskText));
                continue;
            }

            if (logEvent.Properties[name] is ScalarValue { Value: string original }) {
                var masked = Mask(original);
                if (!ReferenceEquals(masked, original))
                    logEvent.AddOrUpdateProperty(propertyFactory.CreateProperty(name, masked));
            }
        }

        // If RenderedMessageEnricher ran first, scrub the rendered text too —
        // it can contain secrets baked into literal message text.
        if (logEvent.Properties.TryGetValue("RenderedMessage", out var rendered) &&
            rendered is ScalarValue { Value: string renderedText }) {
            var masked = Mask(renderedText);
            if (!ReferenceEquals(masked, renderedText))
                logEvent.AddOrUpdateProperty(propertyFactory.CreateProperty("RenderedMessage", masked));
        }
    }

    private string Mask(string input) {
        foreach (var pattern in _patterns) {
            if (pattern.IsMatch(input))
                input = pattern.Replace(input, m => ReplaceMatch(m, _options.MaskText));
        }
        return input;
    }

    private static string ReplaceMatch(Match match, string maskText) {
        var secretGroup = match.Groups[SecretGroupName];
        if (!secretGroup.Success)
            return maskText;

        var offset = secretGroup.Index - match.Index;
        return match.Value[..offset] + maskText + match.Value[(offset + secretGroup.Length)..];
    }
}
