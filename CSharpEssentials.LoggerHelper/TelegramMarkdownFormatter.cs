using Serilog.Events;
using Serilog.Formatting;

namespace CSharpEssentials.LoggerHelper;
public class TelegramMarkdownFormatter : ITextFormatter {
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

        if (logEvent.Properties.ContainsKey("MachineName"))
            output.WriteLine($"`Machine`: {Escape(logEvent.Properties["MachineName"].ToString())}");

        if (logEvent.Properties.ContainsKey("IdTransaction"))
            output.WriteLine($"`Id`: {Escape(logEvent.Properties["IdTransaction"].ToString())}");

        if (logEvent.Properties.ContainsKey("Action"))
            output.WriteLine($"`Action`: {Escape(logEvent.Properties["Action"].ToString())}");
    }

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