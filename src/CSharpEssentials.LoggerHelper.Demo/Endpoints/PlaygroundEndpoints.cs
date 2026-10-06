using CSharpEssentials.LoggerHelper.Diagnostics;
using CSharpEssentials.LoggerHelper.Sink.Elasticsearch;
using CSharpEssentials.LoggerHelper.Sink.File;
using CSharpEssentials.LoggerHelper.Sink.MSSqlServer;
using CSharpEssentials.LoggerHelper.Sink.Postgresql;
using CSharpEssentials.LoggerHelper.Sink.Seq;
using CSharpEssentials.LoggerHelper.Sink.Telegram;
using Serilog.Core;
using Serilog.Debugging;
using Serilog.Events;
using Serilog.Parsing;
using System.Diagnostics;

namespace CSharpEssentials.LoggerHelper.Demo.Endpoints;

/// <summary>
/// Esempio 11: Playground interattivo (pagina /playground.html).
/// Ogni richiesta costruisce un logger NUOVO con la sola API fluente (nessun JSON), in base alle
/// opzioni scelte nella pagina: sink, livello minimo per sink, masking, proprietà. Scrive un evento,
/// fa Dispose (= flush di tutti i sink batch) e restituisce cosa è successo.
/// Infrastruttura: docker/docker-compose.yml. Connessioni: sezione "Playground" di appsettings.json
/// (Telegram: user-secrets "Playground:Telegram:BotToken" / "Playground:Telegram:ChatId").
/// </summary>
public class PlaygroundEndpoints : IEndpointDefinition {
    public static readonly string[] SupportedSinks = ["Console", "File", "Seq", "Elasticsearch", "MSSqlServer", "PostgreSql", "Telegram"];

    // ponytail: one request at a time — SelfLog and the legacy static logger are process-wide,
    // so concurrent playground loggers would steal each other's diagnostics. Fine for a demo.
    internal static readonly SemaphoreSlim Gate = new(1, 1);

    public sealed record PlaygroundRequest(
        string Message,
        string Level,
        Dictionary<string, string> Sinks,                 // sink name → minimum level it accepts
        Dictionary<string, string>? Properties = null,
        PlaygroundMasking? Masking = null,
        bool RenderedMessage = true,
        bool IncludeException = false,
        string? FileNameProperty = null);

    public sealed record PlaygroundMasking(
        bool Enabled,
        List<string>? Presets = null,
        List<string>? SensitiveProperties = null,
        List<string>? Rules = null,
        string? MaskText = null);

    public void DefineEndpoints(WebApplication app) {
        var group = app.MapGroup("/api/playground").WithTags("Playground");

        group.MapGet("/options", (IConfiguration config) => Results.Ok(new {
            sinks = SupportedSinks,
            // Sinks that need secrets nobody should type into a web page: enabled only when configured.
            unavailable = TelegramConfigured(config.GetSection("Playground")) ? [] : new[] {
                new { sink = "Telegram", hint = "dotnet user-secrets set Playground:Telegram:BotToken <token> (and ChatId)" }
            },
            levels = Enum.GetNames<LogEventLevel>(),
            presets = new[] { "Email", "CreditCard", "JwtToken", "BearerToken", "ConnectionStringSecret" }
        }))
        .WithSummary("Sinks, levels and masking presets available in the playground");

        group.MapPost("/log", async (PlaygroundRequest req, IConfiguration config, LoggerHelperOptions hostOptions, ILogErrorStore hostErrors) => {
            if (string.IsNullOrWhiteSpace(req.Message))
                return Results.BadRequest(new { error = "Message is required" });
            if (!Enum.TryParse<LogEventLevel>(req.Level, true, out var level))
                return Results.BadRequest(new { error = $"Unknown level '{req.Level}'" });
            if (req.Sinks is not { Count: > 0 })
                return Results.BadRequest(new { error = "Select at least one sink" });

            await Gate.WaitAsync();
            try {
                var session = new Session(req.Sinks, req.Masking, req.RenderedMessage, req.FileNameProperty,
                                          config.GetSection("Playground"), hostOptions, hostErrors);
                try {
                    Write(session.Logger, req, level);
                } finally {
                    session.Dispose();
                }
                return Results.Ok(session.Report());
            } finally {
                Gate.Release();
            }
        })
        .WithSummary("Build a logger with the fluent API, write one event, flush, report")
        .WithDescription(
            "Builds a brand-new LoggerHelper pipeline from the request using only the fluent API " +
            "(AddRoute, ConfigureSeq/File/Elasticsearch/MSSqlServer/PostgreSql/Telegram, EnableSensitiveDataMasking), " +
            "writes one event, disposes the logger to flush every batching sink and returns: " +
            "which sinks received the event, the event as sinks see it (after masking) and any sink error.");
    }

    private static bool TelegramConfigured(IConfigurationSection cfg) =>
        !string.IsNullOrWhiteSpace(cfg["Telegram:BotToken"]) && !string.IsNullOrWhiteSpace(cfg["Telegram:ChatId"]);

    /// <summary>
    /// One playground logger: built fluently from the page choices, disposed (= flushed) after use.
    /// Shared by the logging and the HttpHelper playground endpoints. Call under <see cref="Gate"/>.
    /// </summary>
    internal sealed class Session : IDisposable {
        private readonly Stopwatch _sw = Stopwatch.StartNew();
        private readonly CaptureSink _capture = new();
        private readonly List<string> _runtimeErrors = [];
        private readonly ServiceProvider _sp;
        private readonly LoggerHelperOptions _hostOptions;
        private readonly ILogErrorStore _hostErrors;
        private bool _disposed;
        private IReadOnlyList<LoadedSinkInfo> _loaded = [];
        private IReadOnlyList<LogErrorEntry> _configErrors = [];

        public Serilog.ILogger Logger { get; }

        public Session(Dictionary<string, string> sinks, PlaygroundMasking? masking, bool renderedMessage, string? fileNameProperty,
                       IConfigurationSection cfg, LoggerHelperOptions hostOptions, ILogErrorStore hostErrors) {
            _hostOptions = hostOptions;
            _hostErrors = hostErrors;

            // ── The point of the demo: the whole pipeline is configured fluently from the UI choices ──
            var services = new ServiceCollection();
            services.AddLoggerHelper(b => {
                b.WithApplicationName("LoggerHelperPlayground")
                 .DisableOpenTelemetry()
                 .WithEnrichers(c => c.WriteTo.Sink(_capture));   // see the events after enrichers/masking

                if (renderedMessage)
                    b.EnableRenderedMessage();

                foreach (var (sink, minLevel) in sinks) {
                    var min = Enum.TryParse<LogEventLevel>(minLevel, true, out var m) ? m : LogEventLevel.Verbose;
                    b.AddRoute(sink, Enum.GetValues<LogEventLevel>().Where(l => l >= min).ToArray());
                }

                b.ConfigureFile(f => {
                    f.Path = Path.Combine("Logs", "playground");
                    f.FileNameProperty = string.IsNullOrWhiteSpace(fileNameProperty) ? null : fileNameProperty;
                })
                 .ConfigureSeq(s => s.ServerUrl = cfg["SeqUrl"] ?? "")
                 .ConfigureElasticsearch(e => {
                     e.NodeUris = cfg["ElasticsearchUrl"] ?? "";
                     e.IndexFormat = "loggerhelper-playground-{0:yyyy.MM.dd}";
                     e.AutoRegisterTemplate = false;   // a new logger per request: skip the template round-trip every time
                 })
                 .ConfigureMSSqlServer(s => {
                     s.ConnectionString = cfg["SqlServer"] ?? "";
                     s.TableName = "PlaygroundLogs";
                     s.BatchPostingLimit = 1;
                 })
                 .ConfigurePostgreSql(p => {
                     p.ConnectionString = cfg["PostgreSql"] ?? "";
                     p.TableName = "playground_logs";
                 })
                 .ConfigureTelegram(t => {
                     t.BotToken = cfg["Telegram:BotToken"] ?? "";
                     t.ChatId = cfg["Telegram:ChatId"] ?? "";
                 });

                if (masking is { Enabled: true } mask)
                    b.EnableSensitiveDataMasking(o => {
                        o.Presets = mask.Presets ?? [];
                        o.SensitiveProperties = mask.SensitiveProperties ?? [];
                        o.Rules = (mask.Rules ?? []).Where(r => !string.IsNullOrWhiteSpace(r))
                            .Select((r, i) => new MaskingRule { Name = $"Rule{i + 1}", Pattern = r }).ToList();
                        if (!string.IsNullOrWhiteSpace(mask.MaskText))
                            o.MaskText = mask.MaskText;
                    });
            });

            // Sink failures at write/flush time only surface through Serilog SelfLog.
            SelfLog.Enable(msg => { lock (_runtimeErrors) _runtimeErrors.Add(msg); });

            _sp = services.BuildServiceProvider();
            Logger = _sp.GetRequiredService<Serilog.ILogger>();
        }

        /// <summary>Flushes every batching sink (MSSqlServer, PostgreSQL, Seq, Elasticsearch) and gives SelfLog back to the host.</summary>
        public void Dispose() {
            if (_disposed)
                return;
            _disposed = true;
            (Logger as IDisposable)?.Dispose();
            _sw.Stop();
            // Our temporary logger hijacked the process-wide SelfLog: give it back to the host pipeline.
            SelfLog.Disable();
            if (_hostOptions.General.EnableSelfLogging)
                SelfLog.Enable(msg => _hostErrors.Add(new LogErrorEntry { SinkName = "SelfLog", ErrorMessage = msg }));
            _loaded = _sp.GetRequiredService<ILoadedSinkStore>().GetAll();
            _configErrors = _sp.GetRequiredService<ILogErrorStore>().GetAll();
            _sp.Dispose();
        }

        /// <summary>
        /// Per-sink outcome (how many of the written events each sink accepted), errors and the events
        /// exactly as the sinks received them. Call after <see cref="Dispose"/>.
        /// </summary>
        public object Report() {
            var events = _capture.Snapshot();
            var evt = events.LastOrDefault();
            List<string> runtimeErrors;
            lock (_runtimeErrors) runtimeErrors = [.. _runtimeErrors];

            return new {
                elapsedMs = _sw.ElapsedMilliseconds,
                level = evt?.Level.ToString(),
                sinks = _loaded.Select(s => {
                    var count = s.Configured ? events.Count(e => s.Levels.Contains(e.Level.ToString(), StringComparer.OrdinalIgnoreCase)) : 0;
                    return new { sink = s.SinkName, configured = s.Configured, levels = s.Levels, received = count > 0, count };
                }),
                errors = _configErrors.Select(e => $"{e.SinkName}: {e.ErrorMessage}")
                    .Concat(runtimeErrors.Select(FirstLine)),
                logEvent = evt is null ? null : Describe(evt),
                events = events.Select(Describe)
            };
        }

        // SelfLog entries carry whole stack traces: the first line says what failed.
        private static string FirstLine(string error) {
            var line = error.Trim().Split('\n')[0].Trim();
            return line.Length > 300 ? line[..300] + "…" : line;
        }

        private static object Describe(LogEvent evt) => new {
            level = evt.Level.ToString(),
            rendered = evt.RenderMessage(),
            template = evt.MessageTemplate.Text,
            properties = evt.Properties.ToDictionary(p => p.Key, p => p.Value is ScalarValue { Value: string s } ? s : p.Value.ToString()),
            exception = evt.Exception?.Message
        };

        // Bounded by what one playground request writes (1 event, or a handful of HTTP attempts).
        private sealed class CaptureSink : ILogEventSink {
            private readonly List<LogEvent> _events = [];
            public void Emit(LogEvent logEvent) { lock (_events) _events.Add(logEvent); }
            public List<LogEvent> Snapshot() { lock (_events) return [.. _events]; }
        }
    }

    private static void Write(Serilog.ILogger logger, PlaygroundRequest req, LogEventLevel level) {
        var props = req.Properties ?? [];

        // Placeholders in the message ({Email}, {Password}...) are bound positionally by Serilog:
        // pass the property values in placeholder order. The other properties go on the context.
        var placeholders = new MessageTemplateParser().Parse(req.Message).Tokens
            .OfType<PropertyToken>().Select(t => t.PropertyName).ToList();
        var args = placeholders.Select(n => (object?)props.GetValueOrDefault(n)).ToArray();

        foreach (var (key, value) in props.Where(p => !placeholders.Contains(p.Key)))
            logger = logger.ForContext(key, value);

        var ex = req.IncludeException ? CreateException() : null;
        logger.Write(level, ex, req.Message, args);
    }

    private static Exception CreateException() {
        try {
            throw new InvalidOperationException("Playground test exception");
        } catch (Exception ex) {
            return ex;
        }
    }
}
