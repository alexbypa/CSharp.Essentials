using Xunit.Abstractions;
using Serilog;
using Serilog.Core;
using Serilog.Events;

#pragma warning disable CS0618 // loggerExtension<T> is obsolete by design

namespace CSharpEssentials.LoggerHelper.Tests;

public class LegacyLoggerExtensionTests : IDisposable {
    private readonly ITestOutputHelper _output;

    private readonly List<LogEvent> _events = [];
    private readonly Serilog.ILogger? _previous = LegacyLoggerHolder.Instance;

    public LegacyLoggerExtensionTests(ITestOutputHelper output) {
        _output = output;
        LegacyLoggerHolder.Instance = new LoggerConfiguration()
            .WriteTo.Sink(new ListSink(_events))
            .CreateLogger();
    }

    public void Dispose() => LegacyLoggerHolder.Instance = _previous;

    private sealed record FakeRequest(string IdTransaction, string Action, string ApplicationName = "App") : IRequest;

    private sealed class ListSink(List<LogEvent> events) : ILogEventSink {
        public void Emit(LogEvent logEvent) { lock (events) events.Add(logEvent); }
    }

    private static string Prop(LogEvent e, string name) =>
        ((ScalarValue)e.Properties[name]).Value!.ToString()!;

    [Fact]
    public void LegacyLoggerRelease_DisposingCurrentLogger_RestoresPreviousOne() {
        _output.WriteLine("[Scenario] Logger legacy A attivo; un secondo logger B (con LegacyLoggerRelease, LevelAlias.Off) diventa legacy e viene poi disposto");
        _output.WriteLine("[Atteso] Dopo Dispose di B, LegacyLoggerHolder.Instance torna ad A; il sink release non riceve eventi");

        var a = LegacyLoggerHolder.Instance;
        var release = new LegacyLoggerRelease(a);
        var b = new LoggerConfiguration().WriteTo.Sink(release, LevelAlias.Off).CreateLogger();
        release.Owner = b;
        LegacyLoggerHolder.Instance = b;

        b.Fatal("ignored");
        b.Dispose();

        _output.WriteLine($"[Restituito] Instance è A: {ReferenceEquals(LegacyLoggerHolder.Instance, a)}");
        Assert.Same(a, LegacyLoggerHolder.Instance);
    }

    [Fact]
    public void LegacyLoggerRelease_DisposingNonCurrentLogger_LeavesInstanceUnchanged() {
        _output.WriteLine("[Scenario] Logger B registrato ma poi sostituito da C come logger legacy; B viene disposto");
        _output.WriteLine("[Atteso] Instance resta C (il release di B non tocca un logger che non è più il suo)");

        var release = new LegacyLoggerRelease(LegacyLoggerHolder.Instance);
        var b = new LoggerConfiguration().WriteTo.Sink(release, LevelAlias.Off).CreateLogger();
        release.Owner = b;
        var c = new LoggerConfiguration().CreateLogger();
        LegacyLoggerHolder.Instance = c;

        b.Dispose();

        _output.WriteLine($"[Restituito] Instance è C: {ReferenceEquals(LegacyLoggerHolder.Instance, c)}");
        Assert.Same(c, LegacyLoggerHolder.Instance);
    }

    [Fact]
    public void TraceSync_IdTransactionAndActionWithCrLf_AreSanitized() {
        _output.WriteLine("[Scenario] TraceSync con IdTransaction e Action contenenti caratteri CR/LF (rischio log forging)");
        _output.WriteLine("[Atteso] CR/LF sostituiti da spazio: IdTransaction=\"tx 1\" e Action=\"act ion\"");

        loggerExtension<FakeRequest>.TraceSync(
            new FakeRequest("tx\r\n1", "act\nion"), LogEventLevel.Information, null, "msg");

        var evt = Assert.Single(_events);
        _output.WriteLine($"[Restituito] IdTransaction=\"{Prop(evt, "IdTransaction")}\", Action=\"{Prop(evt, "Action")}\"");
        Assert.Equal("tx 1", Prop(evt, "IdTransaction"));
        Assert.Equal("act ion", Prop(evt, "Action"));
    }

    [Fact]
    public void TraceSync_IdTransactionAndActionWithUnicodeSeparators_AreSanitized() {
        _output.WriteLine("[Scenario] TraceSync con IdTransaction contenente U+2028 (line separator) e Action contenente U+0085 (next line)");
        _output.WriteLine("[Atteso] I separatori Unicode sono sostituiti da spazio: IdTransaction=\"tx 1\" e Action=\"act ion\"");

        loggerExtension<FakeRequest>.TraceSync(
            new FakeRequest("tx\u20281", "act\u0085ion"), LogEventLevel.Information, null, "msg");

        var evt = Assert.Single(_events);
        _output.WriteLine($"[Restituito] IdTransaction=\"{Prop(evt, "IdTransaction")}\", Action=\"{Prop(evt, "Action")}\"");
        Assert.Equal("tx 1", Prop(evt, "IdTransaction"));
        Assert.Equal("act ion", Prop(evt, "Action"));
    }

    [Fact]
    public void TraceSync_NoLoggerConfigured_DoesNothing() {
        _output.WriteLine("[Scenario] LegacyLoggerHolder.Instance = null (nessun logger legacy configurato) e chiamo TraceSync");
        _output.WriteLine("[Atteso] Nessuna eccezione e nessun evento emesso");

        LegacyLoggerHolder.Instance = null;

        loggerExtension<FakeRequest>.TraceSync(
            new FakeRequest("a", "b"), LogEventLevel.Information, null, "msg");

        _output.WriteLine($"[Restituito] Eventi emessi={_events.Count}");
        Assert.Empty(_events);
    }

    [Fact]
    public void TraceSync_ValidRequest_AddsIdTransactionMachineNameAndAction() {
        _output.WriteLine("[Scenario] TraceSync con request valida (IdTransaction=tx1, Action=act) a livello Warning");
        _output.WriteLine("[Atteso] Un evento Warning arricchito con IdTransaction=\"tx1\", MachineName=Environment.MachineName e Action=\"act\"");

        loggerExtension<FakeRequest>.TraceSync(
            new FakeRequest("tx1", "act"), LogEventLevel.Warning, null, "msg");

        var evt = Assert.Single(_events);
        _output.WriteLine($"[Restituito] Level={evt.Level}, IdTransaction={Prop(evt, "IdTransaction")}, MachineName={Prop(evt, "MachineName")}, Action={Prop(evt, "Action")}");
        Assert.Equal(LogEventLevel.Warning, evt.Level);
        Assert.Equal("tx1", Prop(evt, "IdTransaction"));
        Assert.Equal(Environment.MachineName, Prop(evt, "MachineName"));
        Assert.Equal("act", Prop(evt, "Action"));
    }

    [Fact]
    public void TraceSync_WithUserArgsAndException_BindsArgsAndPropagatesException() {
        _output.WriteLine("[Scenario] TraceSync livello Error con template \"hello {Name}\", argomento \"Bob\" e una InvalidOperationException(\"boom\")");
        _output.WriteLine("[Atteso] Evento con la stessa istanza di eccezione, proprietà Name=\"Bob\" e IdTransaction=\"tx1\"");

        var ex = new InvalidOperationException("boom");

        loggerExtension<FakeRequest>.TraceSync(
            new FakeRequest("tx1", "act"), LogEventLevel.Error, ex, "hello {Name}", "Bob");

        var evt = Assert.Single(_events);
        _output.WriteLine($"[Restituito] Exception={evt.Exception?.Message}, Name={Prop(evt, "Name")}, IdTransaction={Prop(evt, "IdTransaction")}");
        Assert.Same(ex, evt.Exception);
        Assert.Equal("Bob", Prop(evt, "Name"));
        Assert.Equal("tx1", Prop(evt, "IdTransaction"));
    }

    [Fact]
    public void TraceSync_NullRequest_UsesGuidAndUnknown() {
        _output.WriteLine("[Scenario] TraceSync con request null");
        _output.WriteLine("[Atteso] IdTransaction è un Guid generato e Action=\"UNKNOWN\"");

        loggerExtension<FakeRequest>.TraceSync(null!, LogEventLevel.Information, null, "msg");

        var evt = Assert.Single(_events);
        _output.WriteLine($"[Restituito] IdTransaction={Prop(evt, "IdTransaction")}, Action={Prop(evt, "Action")}");
        Assert.True(Guid.TryParse(Prop(evt, "IdTransaction"), out _));
        Assert.Equal("UNKNOWN", Prop(evt, "Action"));
    }

    [Fact]
    public void TraceSync_NullArgs_LogsWithEnrichment() {
        _output.WriteLine("[Scenario] TraceSync con array di argomenti null passato esplicitamente");
        _output.WriteLine("[Atteso] Nessuna eccezione: l'evento viene comunque loggato con IdTransaction=\"tx1\" e Action=\"act\"");

        loggerExtension<FakeRequest>.TraceSync(
            new FakeRequest("tx1", "act"), LogEventLevel.Information, null, "msg", null!);

        var evt = Assert.Single(_events);
        _output.WriteLine($"[Restituito] IdTransaction={Prop(evt, "IdTransaction")}, Action={Prop(evt, "Action")}");
        Assert.Equal("tx1", Prop(evt, "IdTransaction"));
        Assert.Equal("act", Prop(evt, "Action"));
    }

    [Fact]
    public void TraceSync_ActivityWithDisplayName_AddsSpanName() {
        _output.WriteLine("[Scenario] TraceSync mentre è attiva una System.Diagnostics.Activity con DisplayName \"my-op\"");
        _output.WriteLine("[Atteso] L'evento contiene la proprietà SpanName=\"my-op\"");

        using var activity = new System.Diagnostics.Activity("my-op").Start();

        loggerExtension<FakeRequest>.TraceSync(
            new FakeRequest("tx1", "act"), LogEventLevel.Information, null, "msg");

        _output.WriteLine($"[Restituito] Eventi={_events.Count}, SpanName={Prop(_events[0], "SpanName")}");
        Assert.Equal("my-op", Prop(Assert.Single(_events), "SpanName"));
    }

    [Fact]
    public void TraceDashBoardSync_ValidRequest_AddsTargetSinkDashboard() {
        _output.WriteLine("[Scenario] TraceDashBoardSync con request valida (IdTransaction=tx1, Action=act)");
        _output.WriteLine("[Atteso] Evento con TargetSink=\"Dashboard\", IdTransaction=\"tx1\" e Action=\"act\"");

        loggerExtension<FakeRequest>.TraceDashBoardSync(
            new FakeRequest("tx1", "act"), LogEventLevel.Information, null, "msg");

        var evt = Assert.Single(_events);
        _output.WriteLine($"[Restituito] TargetSink={Prop(evt, "TargetSink")}, IdTransaction={Prop(evt, "IdTransaction")}, Action={Prop(evt, "Action")}");
        Assert.Equal("Dashboard", Prop(evt, "TargetSink"));
        Assert.Equal("tx1", Prop(evt, "IdTransaction"));
        Assert.Equal("act", Prop(evt, "Action"));
    }

    [Fact]
    public void TraceDashBoardSync_NullArgs_DoesNotThrowAndAddsTargetSink() {
        _output.WriteLine("[Scenario] TraceDashBoardSync con array di argomenti null");
        _output.WriteLine("[Atteso] Nessuna eccezione e evento con TargetSink=\"Dashboard\"");

        loggerExtension<FakeRequest>.TraceDashBoardSync(
            new FakeRequest("tx1", "act"), LogEventLevel.Information, null, "msg", null!);

        _output.WriteLine($"[Restituito] Eventi={_events.Count}, TargetSink={Prop(_events[0], "TargetSink")}");
        Assert.Equal("Dashboard", Prop(Assert.Single(_events), "TargetSink"));
    }

    [Fact]
    public void TraceAsync_ValidRequest_EventuallyLogsEnrichedEvent() {
        _output.WriteLine("[Scenario] TraceAsync (async void su thread-pool) con request valida IdTransaction=tx1");
        _output.WriteLine("[Atteso] Entro 5 secondi arriva un solo evento con IdTransaction=\"tx1\"");

        loggerExtension<FakeRequest>.TraceAsync(
            new FakeRequest("tx1", "act"), LogEventLevel.Information, null, "msg");

        var evt = WaitForSingleEvent();
        _output.WriteLine($"[Restituito] IdTransaction={Prop(evt, "IdTransaction")}");
        Assert.Equal("tx1", Prop(evt, "IdTransaction"));
    }

    [Fact]
    public void TraceDashBoardAsync_ValidRequest_EventuallyLogsTargetSinkDashboard() {
        _output.WriteLine("[Scenario] TraceDashBoardAsync (async void su thread-pool) con request valida");
        _output.WriteLine("[Atteso] Entro 5 secondi arriva un solo evento con TargetSink=\"Dashboard\"");

        loggerExtension<FakeRequest>.TraceDashBoardAsync(
            new FakeRequest("tx1", "act"), LogEventLevel.Information, null, "msg");

        var evt = WaitForSingleEvent();
        _output.WriteLine($"[Restituito] TargetSink={Prop(evt, "TargetSink")}");
        Assert.Equal("Dashboard", Prop(evt, "TargetSink"));
    }

    [Fact]
    public void TraceDashBoardAsync_NullArgs_DoesNotCrashAndLogsTargetSink() {
        _output.WriteLine("[Scenario] TraceDashBoardAsync (async void) con array di argomenti null");
        _output.WriteLine("[Atteso] Nessun crash: entro 5 secondi arriva un solo evento con TargetSink=\"Dashboard\"");

        loggerExtension<FakeRequest>.TraceDashBoardAsync(
            new FakeRequest("tx1", "act"), LogEventLevel.Information, null, "msg", null!);

        var evt = WaitForSingleEvent();
        _output.WriteLine($"[Restituito] TargetSink={Prop(evt, "TargetSink")}");
        Assert.Equal("Dashboard", Prop(evt, "TargetSink"));
    }

    [Fact]
    public void TraceDashBoardSync_WithUserArgs_BindsUserArgsAndTargetSink() {
        _output.WriteLine("[Scenario] TraceDashBoardSync con template \"hi {Name}\" e argomento \"Bob\"");
        _output.WriteLine("[Atteso] Evento con Name=\"Bob\" e TargetSink=\"Dashboard\"");

        loggerExtension<FakeRequest>.TraceDashBoardSync(
            new FakeRequest("tx1", "act"), LogEventLevel.Information, null, "hi {Name}", "Bob");

        var evt = Assert.Single(_events);
        _output.WriteLine($"[Restituito] Name={Prop(evt, "Name")}, TargetSink={Prop(evt, "TargetSink")}");
        Assert.Equal("Bob", Prop(evt, "Name"));
        Assert.Equal("Dashboard", Prop(evt, "TargetSink"));
    }

    [Fact]
    public void TraceDashBoardSync_NoLoggerConfigured_DoesNothing() {
        _output.WriteLine("[Scenario] LegacyLoggerHolder.Instance = null (nessun logger legacy configurato) e chiamo TraceDashBoardSync");
        _output.WriteLine("[Atteso] Nessuna eccezione e nessun evento emesso");

        LegacyLoggerHolder.Instance = null;

        loggerExtension<FakeRequest>.TraceDashBoardSync(
            new FakeRequest("a", "b"), LogEventLevel.Information, null, "msg");

        _output.WriteLine($"[Restituito] Eventi emessi={_events.Count}");
        Assert.Empty(_events);
    }

    // The async variants are async void: poll until the thread-pool write lands.
    private LogEvent WaitForSingleEvent() {
        Assert.True(SpinWait.SpinUntil(() => { lock (_events) return _events.Count == 1; }, TimeSpan.FromSeconds(5)));
        lock (_events) return _events.Single();
    }
}
