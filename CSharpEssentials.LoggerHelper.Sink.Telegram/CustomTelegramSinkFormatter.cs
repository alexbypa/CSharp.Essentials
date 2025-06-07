using Serilog.Events;
using Serilog.Formatting;

namespace CSharpEssentials.LoggerHelper.Sink.Telegram;
/// <summary>
/// Custom formatter for Telegram output using Markdown syntax.
/// Displays log level with emoji, timestamp, message, exception (if any),
/// and all properties enriched via ForContext.
/// </summary>
internal class CustomTelegramSinkFormatter : ITextFormatter {
    /// <summary>
    /// Formats the log event for output to Telegram (Markdown-escaped).
    /// </summary>
    public void Format(LogEvent logEvent, TextWriter output) {
        string emoji = logEvent.Level switch {
            LogEventLevel.Information => "\u2139\ufe0f", // ℹ️
            LogEventLevel.Warning => "\u26a0\ufe0f", // ⚠️
            LogEventLevel.Error => "\u274c",        // ❌
            LogEventLevel.Fatal => "\ud83d\udd25", // 🔥
            LogEventLevel.Debug => "\ud83d\udc1e", // 🐞
            _ => "\ud83d\udd39"  // 🔹
        };

        string message = Escape(logEvent.RenderMessage());

        output.WriteLine($"{emoji} *{logEvent.Level}* at `{logEvent.Timestamp:yyyy-MM-dd HH:mm:ss}`");
        output.WriteLine(message);

        if (logEvent.Exception != null) {
            output.WriteLine();
            output.WriteLine("*Exception:*");
            output.WriteLine("```");
            output.WriteLine(Escape(logEvent.Exception.ToString()));
            output.WriteLine("```");
        }

        foreach (var prop in logEvent.Properties) {
            output.WriteLine($"`{prop.Key}`: {Escape(prop.Value.ToString())}");
        }
    }
    /// <summary>
    /// Escapes Markdown special characters to avoid formatting issues in Telegram.
    /// </summary>
    private string Escape(string input) {
        return input
            .Replace("_", "\\_")
            .Replace("*", "\\*")
            .Replace("[", "\\[")
            .Replace("]", "\\]")
            .Replace("(", "\\(")
            .Replace(")", "\\)")
            .Replace("~", "\\~")
            .Replace("`", "\\`")
            .Replace(">", "\\>")
            .Replace("#", "\\#")
            .Replace("+", "\\+")
            .Replace("-", "\\-")
            .Replace("=", "\\=")
            .Replace("|", "\\|")
            .Replace("{", "\\{")
            .Replace("}", "\\}")
            .Replace(".", "\\.")
            .Replace("!", "\\!");
    }
}