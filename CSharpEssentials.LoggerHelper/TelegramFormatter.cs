using Serilog.Events;
using Serilog.Formatting;

public class TelegramFormatter : ITextFormatter {
    public void Format(LogEvent logEvent, TextWriter output) {
        string emoji = logEvent.Level switch {
            LogEventLevel.Information => "ℹ️",
            LogEventLevel.Warning => "⚠️",
            LogEventLevel.Error => "❌",
            LogEventLevel.Fatal => "🔥",
            LogEventLevel.Debug => "🐞",
            _ => "📌"
        };

        var time = logEvent.Timestamp.ToString("yyyy-MM-dd HH:mm:ss");

        output.WriteLine($"{emoji} [{logEvent.Level}] {logEvent.RenderMessage()}");
        output.WriteLine($"🕒 {time}");

        if (logEvent.Exception != null) {
            output.WriteLine("❗ Eccezione:");
            output.WriteLine(logEvent.Exception);
        }

        output.WriteLine($"🧾 Id: {logEvent.Properties.GetValueOrDefault("IdTransaction")}");
        output.WriteLine($"📍 Machine: {logEvent.Properties.GetValueOrDefault("MachineName")}");
        output.WriteLine($"🎯 Action: {logEvent.Properties.GetValueOrDefault("Action")}");
    }
}
