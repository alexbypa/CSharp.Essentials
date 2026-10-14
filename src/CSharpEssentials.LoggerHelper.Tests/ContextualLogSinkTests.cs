using Xunit.Abstractions;
using CSharpEssentials.LoggerHelper.Diagnostics;
using Serilog;
using Serilog.Core;
using Serilog.Events;
using Serilog.Parsing;

namespace CSharpEssentials.LoggerHelper.Tests;

public sealed class ContextualLogSinkTests {
    private readonly ITestOutputHelper _output;

    public ContextualLogSinkTests(ITestOutputHelper output) {
        _output = output;
    }

    private sealed class ListSink(List<LogEvent> events) : ILogEventSink {
        public void Emit(LogEvent logEvent) => events.Add(logEvent);
    }

    private readonly List<LogEvent> _replayed = [];
    private readonly ContextualLogBuffer _buffer = new(10);

    private LoggerHolder HolderWithLogger() => new() {
        Logger = new LoggerConfiguration().WriteTo.Sink(new ListSink(_replayed)).CreateLogger()
    };

    private static LogEvent Evt(LogEventLevel level, string message, string? sourceContext = "My.Ctx", bool? history = null) {
        var props = new List<LogEventProperty>();
        if (sourceContext is not null)
            props.Add(new LogEventProperty("SourceContext", new ScalarValue(sourceContext)));
        if (history is not null)
            props.Add(new LogEventProperty("IsContextualHistory", new ScalarValue(history.Value)));
        return new LogEvent(DateTimeOffset.UtcNow, level, null, new MessageTemplateParser().Parse(message), props);
    }

    private static string Scalar(LogEvent e, string name) =>
        ((ScalarValue)e.Properties[name]).Value!.ToString()!;

    [Theory]
    [InlineData(LogEventLevel.Debug)]
    [InlineData(LogEventLevel.Information)]
    [InlineData(LogEventLevel.Warning)]
    public void Emit_CapturedLevel_PushesToBuffer(LogEventLevel level) {
        _output.WriteLine($"[Scenario] ContextualLogSink con livelli catturati di default: emetto un evento {level} \"hello\" con SourceContext \"My.Ctx\"");
        _output.WriteLine($"[Atteso] L'evento viene accodato nel buffer contestuale (Level={level}, Message=\"hello\", SourceContext=\"My.Ctx\")");

        var sink = new ContextualLogSink(_buffer, new LoggerHolder());

        sink.Emit(Evt(level, "hello"));

        var entry = Assert.Single(_buffer.Snapshot());
        _output.WriteLine($"[Restituito] Level={entry.Level}, Message=\"{entry.Message}\", SourceContext=\"{entry.SourceContext}\"");
        Assert.Equal(level, entry.Level);
        Assert.Equal("hello", entry.Message);
        Assert.Equal("My.Ctx", entry.SourceContext);
    }

    [Fact]
    public void Emit_VerboseByDefault_IsNotBuffered() {
        _output.WriteLine("[Scenario] ContextualLogSink con configurazione di default: emetto un evento Verbose");
        _output.WriteLine("[Atteso] Verbose non fa parte dei livelli catturati di default: il buffer resta vuoto (Count == 0)");

        var sink = new ContextualLogSink(_buffer, new LoggerHolder());

        sink.Emit(Evt(LogEventLevel.Verbose, "v"));

        _output.WriteLine($"[Restituito] Count={_buffer.Count}");
        Assert.Equal(0, _buffer.Count);
    }

    [Fact]
    public void Emit_CustomCapturedLevels_OnlyThoseBuffered() {
        _output.WriteLine("[Scenario] ContextualLogSink configurato per catturare solo Verbose: emetto un evento Verbose \"v\" e uno Information \"i\"");
        _output.WriteLine("[Atteso] Solo l'evento Verbose viene accodato nel buffer (1 elemento, Message=\"v\")");

        var sink = new ContextualLogSink(_buffer, new LoggerHolder(), [LogEventLevel.Verbose]);

        sink.Emit(Evt(LogEventLevel.Verbose, "v"));
        sink.Emit(Evt(LogEventLevel.Information, "i"));

        var entry = Assert.Single(_buffer.Snapshot());
        _output.WriteLine($"[Restituito] Count={_buffer.Count}, Message=\"{entry.Message}\"");
        Assert.Equal("v", entry.Message);
    }

    [Theory]
    [InlineData(LogEventLevel.Error)]
    [InlineData(LogEventLevel.Fatal)]
    public void Emit_ErrorOrFatal_FlushesContextThroughLogger(LogEventLevel level) {
        _output.WriteLine($"[Scenario] Buffer con 2 eventi di contesto (Debug da Ctx.A, Warning da Ctx.B), poi emetto un evento {level} \"kaboom\"");
        _output.WriteLine($"[Atteso] Il buffer viene svuotato e i 2 eventi sono rigiocati sul logger come Error con IsContextualHistory=true, OriginalLevel/OriginalSourceContext preservati; LastFlush.TriggeringError=\"kaboom\" di livello {level}");

        var sink = new ContextualLogSink(_buffer, HolderWithLogger());
        sink.Emit(Evt(LogEventLevel.Debug, "d1", "Ctx.A"));
        sink.Emit(Evt(LogEventLevel.Warning, "w2", "Ctx.B"));

        sink.Emit(Evt(level, "kaboom"));
        _output.WriteLine($"[Restituito] BufferCount={_buffer.Count}, Replayed={_replayed.Count}, Replay[0]={Scalar(_replayed[0], "OriginalLevel")}/{Scalar(_replayed[0], "OriginalSourceContext")}, Replay[1]={Scalar(_replayed[1], "OriginalLevel")}/{Scalar(_replayed[1], "OriginalSourceContext")}, TriggeringError=\"{_buffer.LastFlush?.TriggeringError?.Message}\" ({_buffer.LastFlush?.TriggeringError?.Level})");

        Assert.Equal(0, _buffer.Count);
        Assert.Equal(2, _replayed.Count);
        Assert.All(_replayed, e => {
            Assert.Equal(LogEventLevel.Error, e.Level);
            Assert.Equal(true, ((ScalarValue)e.Properties["IsContextualHistory"]).Value);
        });
        Assert.Equal("Debug", Scalar(_replayed[0], "OriginalLevel"));
        Assert.Equal("Ctx.A", Scalar(_replayed[0], "OriginalSourceContext"));
        Assert.Equal("Warning", Scalar(_replayed[1], "OriginalLevel"));
        Assert.Equal("Ctx.B", Scalar(_replayed[1], "OriginalSourceContext"));
        Assert.Equal("kaboom", _buffer.LastFlush!.TriggeringError!.Message);
        Assert.Equal(level, _buffer.LastFlush.TriggeringError.Level);
    }

    [Fact]
    public void Emit_ErrorWithEmptyBuffer_SetsTriggeringErrorAndWritesNothing() {
        _output.WriteLine("[Scenario] Buffer contestuale vuoto: emetto direttamente un evento Error \"kaboom\"");
        _output.WriteLine("[Atteso] Nessun evento rigiocato sul logger; LastFlush ha TriggeringError=\"kaboom\" e zero Entries");

        var sink = new ContextualLogSink(_buffer, HolderWithLogger());

        sink.Emit(Evt(LogEventLevel.Error, "kaboom"));

        _output.WriteLine($"[Restituito] Replayed={_replayed.Count}, TriggeringError=\"{_buffer.LastFlush?.TriggeringError?.Message}\", FlushEntries={_buffer.LastFlush?.Entries.Count}");
        Assert.Empty(_replayed);
        Assert.Equal("kaboom", _buffer.LastFlush!.TriggeringError!.Message);
        Assert.Empty(_buffer.LastFlush.Entries);
    }

    [Fact]
    public void Emit_ErrorWithNullLogger_DoesNotFlush() {
        _output.WriteLine("[Scenario] LoggerHolder senza logger: accodo un evento Information e poi emetto un Error \"kaboom\"");
        _output.WriteLine("[Atteso] Senza logger il contesto non viene scaricato: il buffer conserva 1 elemento e LastFlush resta null (comportamento corrente fissato dal test)");

        // Pins current behavior: without a logger the context stays buffered.
        var sink = new ContextualLogSink(_buffer, new LoggerHolder());
        sink.Emit(Evt(LogEventLevel.Information, "i"));

        sink.Emit(Evt(LogEventLevel.Error, "kaboom"));

        _output.WriteLine($"[Restituito] BufferCount={_buffer.Count}, LastFlush={(_buffer.LastFlush is null ? "null" : "presente")}");
        Assert.Equal(1, _buffer.Count);
        Assert.Null(_buffer.LastFlush);
    }

    [Fact]
    public void Emit_ContextualHistoryEvent_IsIgnored() {
        _output.WriteLine("[Scenario] Buffer con 1 evento Information; emetto un evento Error già marcato IsContextualHistory=true (replay di un flush precedente)");
        _output.WriteLine("[Atteso] L'evento di replay viene ignorato: nessun nuovo flush, buffer invariato (1 elemento), nulla rigiocato sul logger (evita loop infiniti)");

        var sink = new ContextualLogSink(_buffer, HolderWithLogger());
        sink.Emit(Evt(LogEventLevel.Information, "i"));

        sink.Emit(Evt(LogEventLevel.Error, "replayed", history: true));

        _output.WriteLine($"[Restituito] BufferCount={_buffer.Count}, LastFlush={(_buffer.LastFlush is null ? "null" : "presente")}, Replayed={_replayed.Count}");
        Assert.Equal(1, _buffer.Count);
        Assert.Null(_buffer.LastFlush);
        Assert.Empty(_replayed);
    }

    [Fact]
    public void Emit_MissingSourceContext_UsesUnknownInReplay() {
        _output.WriteLine("[Scenario] Evento Information senza SourceContext accodato nel buffer, poi emetto un Error \"kaboom\" senza SourceContext");
        _output.WriteLine("[Atteso] Nel buffer SourceContext resta null; nel replay OriginalSourceContext=\"Unknown\"");

        var sink = new ContextualLogSink(_buffer, HolderWithLogger());
        sink.Emit(Evt(LogEventLevel.Information, "i", sourceContext: null));
        Assert.Null(Assert.Single(_buffer.Snapshot()).SourceContext);

        sink.Emit(Evt(LogEventLevel.Error, "kaboom", sourceContext: null));

        var replay = Assert.Single(_replayed);
        _output.WriteLine($"[Restituito] OriginalSourceContext=\"{Scalar(replay, "OriginalSourceContext")}\"");
        Assert.Equal("Unknown", Scalar(replay, "OriginalSourceContext"));
    }
}
