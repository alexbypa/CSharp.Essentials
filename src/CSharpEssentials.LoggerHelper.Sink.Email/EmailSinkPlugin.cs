using CSharpEssentials.LoggerHelper.Diagnostics;
using Serilog;
using Serilog.Core;
using Serilog.Debugging;
using Serilog.Events;
using System.Net;
using System.Net.Mail;
using System.Runtime.CompilerServices;

namespace CSharpEssentials.LoggerHelper.Sink.Email;

// ── Options ───────────────────────────────────────────────────────

public sealed class EmailSinkOptions {
    public string From { get; set; } = string.Empty;
    public string To { get; set; } = string.Empty;
    public string Host { get; set; } = string.Empty;
    public int Port { get; set; } = 587;
    public string? Username { get; set; }
    public string? Password { get; set; }
    public bool EnableSsl { get; set; } = true;
    public string? TemplatePath { get; set; }
    public TimeSpan? ThrottleInterval { get; set; }
}

// ── Builder extension ─────────────────────────────────────────────

public static class EmailBuilderExtensions {
    public static LoggerHelperBuilder ConfigureEmail(this LoggerHelperBuilder builder, Action<EmailSinkOptions> configure)
        => builder.ConfigureSink("Email", configure);
}

// ── Plugin ────────────────────────────────────────────────────────

internal sealed class EmailSinkPlugin : ISinkPlugin {
    public bool CanHandle(string sinkName) =>
        string.Equals(sinkName, "Email", StringComparison.OrdinalIgnoreCase);

    public void Configure(LoggerConfiguration loggerConfig, SinkRouting routing, LoggerHelperOptions options) {
        var opts = options.GetSinkConfig<EmailSinkOptions>("Email")
                   ?? options.BindSinkSection<EmailSinkOptions>("Email");
        if (opts is null) {
            SelfLog.WriteLine("Email sink configured in routes but no Sinks.Email options provided.");
            return;
        }

        loggerConfig.WriteTo.Conditional(
            evt => routing.Matches(evt.Level),
            wt => wt.Sink(new EmailLogEventSink(opts))
        );
    }
}

// ── Sink implementation ───────────────────────────────────────────

internal sealed class EmailLogEventSink : ILogEventSink {
    private readonly EmailSinkOptions _opts;
    private readonly string _defaultTemplate;

    internal EmailLogEventSink(EmailSinkOptions opts) {
        _opts = opts;
        _defaultTemplate = LoadDefaultTemplate();
    }

    public void Emit(LogEvent logEvent) {
        try {
            if (_opts.ThrottleInterval.HasValue &&
                !SinkThrottlingManager.CanSend("Email", _opts.ThrottleInterval.Value)) {
                SelfLog.WriteLine($"Email throttled: {logEvent.RenderMessage()}");
                return;
            }

            var htmlBody = GenerateHtmlBody(logEvent);
            var subject = $"[LoggerHelper] [{logEvent.Level}]";

            using var message = new MailMessage(_opts.From, _opts.To, subject, htmlBody) {
                IsBodyHtml = true
            };

            using var smtpClient = new SmtpClient(_opts.Host, _opts.Port) {
                EnableSsl = _opts.EnableSsl,
                Credentials = new NetworkCredential(_opts.Username, _opts.Password)
            };

            smtpClient.Send(message);
        } catch (Exception ex) {
            SelfLog.WriteLine($"Error sending email: {ex}");
        }
    }

    private string GenerateHtmlBody(LogEvent logEvent) {
        var template = !string.IsNullOrWhiteSpace(_opts.TemplatePath) && File.Exists(_opts.TemplatePath)
            ? File.ReadAllText(_opts.TemplatePath)
            : _defaultTemplate;

        template = template
            .Replace("{{Timestamp}}", logEvent.Timestamp.ToString("yyyy-MM-dd HH:mm:ss"))
            .Replace("{{Level}}", logEvent.Level.ToString())
            .Replace("{{LevelClass}}", GetLevelClass(logEvent.Level))
            .Replace("{{Message}}", WebUtility.HtmlEncode(logEvent.RenderMessage()));

        foreach (var prop in logEvent.Properties)
            template = template.Replace($"{{{{{prop.Key}}}}}", WebUtility.HtmlEncode(prop.Value?.ToString()));

        return template;
    }

    private static string GetLevelClass(LogEventLevel level) => level switch {
        LogEventLevel.Information => "level-info",
        LogEventLevel.Warning => "level-warning",
        LogEventLevel.Error or LogEventLevel.Fatal => "level-error",
        _ => ""
    };

    private static string LoadDefaultTemplate() => """
        <html>
        <head>
          <style>
            body { font-family: Arial, sans-serif; margin: 20px; }
            .header { font-size: 24px; font-weight: bold; color: green; }
            .section { margin-top: 20px; }
            .label { font-weight: bold; color: #555; }
            .level-info { color: green; font-weight: bold; }
            .level-warning { color: orange; font-weight: bold; }
            .level-error { color: red; font-weight: bold; }
            table { width: 100%; border-collapse: collapse; margin-top: 10px; }
            td, th { border: 1px solid #ddd; padding: 8px; }
            th { background-color: #f2f2f2; }
            .highlight { background-color: #e8f5e9; padding: 8px; border-radius: 5px; }
          </style>
        </head>
        <body>
          <div class='header'>LoggerHelper Notification</div>
          <div class='section'>
            <span class='label'>Timestamp:</span> {{Timestamp}}<br/>
            <span class='label'>Level:</span> <span class='{{LevelClass}}'>{{Level}}</span>
          </div>
          <div class='section'>
            <table>
              <tr><th>ApplicationName</th><td>{{ApplicationName}}</td></tr>
              <tr><th>MachineName</th><td>{{MachineName}}</td></tr>
            </table>
          </div>
          <div class='section highlight'>
            <h3>Log Message</h3>
            <pre>{{Message}}</pre>
          </div>
        </body>
        </html>
        """;
}

public static class PluginInitializer {
    [ModuleInitializer]
    public static void Init() => SinkPluginRegistry.Register(new EmailSinkPlugin());
}
