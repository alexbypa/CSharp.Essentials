using CSharpEssentials.LoggerHelper.Diagnostics;
using Serilog;
using Serilog.Core;
using Serilog.Debugging;
using Serilog.Events;
using System.Runtime.CompilerServices;

namespace CSharpEssentials.LoggerHelper.Sink.Telegram;

// ── Options ───────────────────────────────────────────────────────

public sealed class TelegramSinkOptions {
    public string BotToken { get; set; } = string.Empty;
    public string ChatId { get; set; } = string.Empty;

    /// <summary>Legacy JSON key: Api_Key</summary>
    public string? Api_Key { set => BotToken = value ?? BotToken; }

    /// <summary>Legacy JSON key: chatId</summary>
    public string? chatId { set => ChatId = value ?? ChatId; }

    /// <summary>
    /// Minimum interval between Telegram messages. Default: 1 second.
    /// JSON example: "00:00:20"
    /// </summary>
    public TimeSpan? ThrottleInterval { get; set; }
}

// ── Builder extension ─────────────────────────────────────────────

public static class TelegramBuilderExtensions {
    public static LoggerHelperBuilder ConfigureTelegram(this LoggerHelperBuilder builder, Action<TelegramSinkOptions> configure)
        => builder.ConfigureSink("Telegram", configure);
}

// ── Plugin ────────────────────────────────────────────────────────

[LoggerHelperSink]
public sealed class TelegramSinkPlugin : ISinkPlugin {
    public bool CanHandle(string sinkName) =>
        string.Equals(sinkName, "Telegram", StringComparison.OrdinalIgnoreCase);

    public void Configure(LoggerConfiguration loggerConfig, SinkRouting routing, LoggerHelperOptions options) {
        var opts = options.GetSinkConfig<TelegramSinkOptions>("Telegram")
                   ?? options.BindSinkSection<TelegramSinkOptions>("Telegram")
                   ?? options.BindSinkSection<TelegramSinkOptions>("TelegramOption");
        if (opts is null) {
            SelfLog.WriteLine("Telegram sink configured in routes but no Sinks.Telegram options provided.");
            return;
        }

        loggerConfig.WriteTo.Conditional(
            evt => routing.Matches(evt.Level),
            wt => wt.Sink(new TelegramLogEventSink(opts))
        );
    }
}

// ── Sink implementation ───────────────────────────────────────────

internal sealed class TelegramLogEventSink : ILogEventSink {
    private readonly TelegramSinkOptions _opts;
    private static readonly HttpClient _client = new() { Timeout = TimeSpan.FromSeconds(10) };

    internal TelegramLogEventSink(TelegramSinkOptions opts) {
        _opts = opts;
    }

    public void Emit(LogEvent logEvent) {
        var message = FormatMessage(logEvent);

        var throttle = _opts.ThrottleInterval ?? TimeSpan.FromSeconds(1);
        if (!SinkThrottlingManager.CanSend("Telegram", throttle)) {
            SelfLog.WriteLine($"Telegram throttled: {logEvent.RenderMessage()}");
            return;
        }

        // Fire-and-forget: Serilog's sink Emit() is synchronous by contract; dispatching to
        // a thread-pool thread avoids blocking Serilog's background queue for a network round-trip.
        // Errors are captured via SelfLog rather than surfaced to the caller!
        _ = Task.Run(async () => {
            try { await SendMessageAsync(message).ConfigureAwait(false); }
            catch (Exception ex) { SelfLog.WriteLine($"Error sending Telegram message: {ex.Message}"); }
        });
    }

    private async Task SendMessageAsync(string message) {
        var url = $"https://api.telegram.org/bot{_opts.BotToken}/sendMessage";
        var data = new Dictionary<string, string> {
            { "chat_id", _opts.ChatId },
            { "text", message },
            { "parse_mode", "MarkdownV2" }
        };

        var response = await _client.PostAsync(url, new FormUrlEncodedContent(data));
        if (!response.IsSuccessStatusCode) {
            var error = await response.Content.ReadAsStringAsync();
            SelfLog.WriteLine($"Telegram API error: {error}");
        }
    }

    private static string FormatMessage(LogEvent logEvent) {
        var emoji = logEvent.Level switch {
            LogEventLevel.Information => "\u2139\ufe0f",
            LogEventLevel.Warning => "\u26a0\ufe0f",
            LogEventLevel.Error => "\u274c",
            LogEventLevel.Fatal => "\ud83d\udd25",
            LogEventLevel.Debug => "\ud83d\udc1e",
            _ => "\ud83d\udd39"
        };

        var msg = Escape(logEvent.RenderMessage());
        var result = $"{emoji} *{logEvent.Level}* at `{logEvent.Timestamp:yyyy\\-MM\\-dd HH:mm:ss}`\n{msg}";

        if (logEvent.Exception is not null)
            result += $"\n\n*Exception:*\n```\n{Escape(logEvent.Exception.ToString())}\n```";

        foreach (var prop in logEvent.Properties)
            result += $"\n`{prop.Key}`: {Escape(prop.Value.ToString())}";

        return result;
    }

    private static string Escape(string input) =>
        input.Replace("_", "\\_").Replace("*", "\\*").Replace("[", "\\[")
             .Replace("]", "\\]").Replace("(", "\\(").Replace(")", "\\)")
             .Replace("~", "\\~").Replace("`", "\\`").Replace(">", "\\>")
             .Replace("#", "\\#").Replace("+", "\\+").Replace("-", "\\-")
             .Replace("=", "\\=").Replace("|", "\\|").Replace("{", "\\{")
             .Replace("}", "\\}").Replace(".", "\\.").Replace("!", "\\!");
}

public static class PluginInitializer {
    [ModuleInitializer]
    public static void Init() => SinkPluginRegistry.Register(new TelegramSinkPlugin());
}