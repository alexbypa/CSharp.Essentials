using Xunit.Abstractions;
using System.Collections.Concurrent;
using System.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Serilog.Events;

namespace CSharpEssentials.LoggerHelper.Tests;

/// <summary>
/// Tests for LoggerExtensions: BeginTrace (scope) and Trace (single-shot).
/// </summary>
public class LoggerExtensionsTests {
    private readonly ITestOutputHelper _output;

    public LoggerExtensionsTests(ITestOutputHelper output) {
        _output = output;
    }

    private static (ILogger logger, CapturingSink sink, ServiceProvider sp) CreateCapturingLogger() {
        var sink = new CapturingSink();
        var services = new ServiceCollection();
        services.AddLoggerHelper(b => b
            .WithApplicationName("TestApp")
            .DisableOpenTelemetry()
            .AddRoute("Null", LogEventLevel.Verbose, LogEventLevel.Debug,
                LogEventLevel.Information, LogEventLevel.Warning,
                LogEventLevel.Error, LogEventLevel.Fatal)
            .WithEnrichers(lc => lc.WriteTo.Sink(sink)));
        var sp = services.BuildServiceProvider();
        var logger = sp.GetRequiredService<ILoggerProvider>().CreateLogger("Test");
        return (logger, sink, sp);
    }

    // ── BeginTrace (scope-based) ───────────────────────────────────

    [Fact]
    public void BeginTrace_AllLogsInScopeHaveIdTransactionAndAction() {
        _output.WriteLine("[Scenario] BeginTrace(\"OrderProcess\", \"TXN-100\"): dentro lo scope scrivo 3 log (Information, Warning, Error)");
        _output.WriteLine("[Atteso] Tutti e 3 gli eventi catturati hanno le proprietà IdTransaction (contiene TXN-100) e Action (contiene OrderProcess)");

        var (logger, sink, sp) = CreateCapturingLogger();
        using (sp) {
            using (logger.BeginTrace("OrderProcess", "TXN-100")) {
                logger.LogInformation("Step 1: validate");
                logger.LogWarning("Step 2: low stock");
                logger.LogError("Step 3: payment failed");
            }

            _output.WriteLine($"[Restituito] Eventi={sink.Events.Count}, IdTransaction=[{string.Join(", ", sink.Events.Select(e => e.Properties.GetValueOrDefault("IdTransaction")))}], Action=[{string.Join(", ", sink.Events.Select(e => e.Properties.GetValueOrDefault("Action")))}]");
            Assert.Equal(3, sink.Events.Count);
            foreach (var evt in sink.Events) {
                Assert.True(evt.Properties.ContainsKey("IdTransaction"),
                    "IdTransaction missing from log event");
                Assert.True(evt.Properties.ContainsKey("Action"),
                    "Action missing from log event");
                Assert.Contains("TXN-100", evt.Properties["IdTransaction"].ToString());
                Assert.Contains("OrderProcess", evt.Properties["Action"].ToString());
            }
        }
    }

    [Fact]
    public void BeginTrace_LogsOutsideScopeDoNotHaveEnrichment() {
        _output.WriteLine("[Scenario] Un log scritto dentro BeginTrace(\"Inside\", \"TXN-IN\") e uno scritto dopo la chiusura dello scope");
        _output.WriteLine("[Atteso] Il primo evento ha IdTransaction, il secondo (fuori scope) no: lo scope non perde contesto");

        var (logger, sink, sp) = CreateCapturingLogger();
        using (sp) {
            using (logger.BeginTrace("Inside", "TXN-IN")) {
                logger.LogInformation("inside scope");
            }
            logger.LogInformation("outside scope");

            _output.WriteLine($"[Restituito] Eventi={sink.Events.Count}, DentroScope.HaIdTransaction={sink.Events[0].Properties.ContainsKey("IdTransaction")}, FuoriScope.HaIdTransaction={sink.Events[1].Properties.ContainsKey("IdTransaction")}");
            Assert.Equal(2, sink.Events.Count);
            // First log (inside) has enrichment
            Assert.True(sink.Events[0].Properties.ContainsKey("IdTransaction"));
            // Second log (outside) does NOT
            Assert.False(sink.Events[1].Properties.ContainsKey("IdTransaction"));
        }
    }

    [Fact]
    public void BeginTrace_IncludesSpanNameWhenActivityIsActive() {
        _output.WriteLine("[Scenario] Activity \"TestOp\" attiva mentre scrivo un log dentro BeginTrace(\"MyAction\", \"TXN-001\")");
        _output.WriteLine("[Atteso] L'evento contiene SpanName che include \"TestOp\" (correlazione con OpenTelemetry)");

        var (logger, sink, sp) = CreateCapturingLogger();
        using (sp) {
            using var activity = new Activity("TestOp").Start();
            using (logger.BeginTrace("MyAction", "TXN-001")) {
                logger.LogInformation("with span");
            }

            _output.WriteLine($"[Restituito] Eventi={sink.Events.Count}, SpanName={sink.Events[0].Properties.GetValueOrDefault("SpanName")}");
            Assert.Single(sink.Events);
            Assert.True(sink.Events[0].Properties.ContainsKey("SpanName"));
            Assert.Contains("TestOp", sink.Events[0].Properties["SpanName"].ToString());
        }
    }

    [Fact]
    public void BeginTrace_NoSpanNameWhenNoActivity() {
        _output.WriteLine("[Scenario] Nessuna Activity attiva: scrivo un log dentro BeginTrace(\"MyAction\", \"TXN-002\")");
        _output.WriteLine("[Atteso] L'evento non contiene la proprietà SpanName");

        var (logger, sink, sp) = CreateCapturingLogger();
        using (sp) {
            Activity.Current?.Stop();
            using (logger.BeginTrace("MyAction", "TXN-002")) {
                logger.LogInformation("no span");
            }

            _output.WriteLine($"[Restituito] Eventi={sink.Events.Count}, HaSpanName={sink.Events[0].Properties.ContainsKey("SpanName")}");
            Assert.Single(sink.Events);
            Assert.False(sink.Events[0].Properties.ContainsKey("SpanName"));
        }
    }

    [Fact]
    public void BeginTrace_WorksWithAllLogLevels() {
        _output.WriteLine("[Scenario] Dentro BeginTrace scrivo Information, Warning, Error (con eccezione) e Critical (con eccezione)");
        _output.WriteLine("[Atteso] Arrivano 4 eventi con livelli Information, Warning, Error, Fatal; gli eventi Error mantengono l'eccezione \"test\" (Debug/Verbose sono filtrati dal MinimumLevel di default)");

        var (logger, sink, sp) = CreateCapturingLogger();
        using (sp) {
            var ex = new InvalidOperationException("test");
            using (logger.BeginTrace("MyAction", "TXN-003")) {
                // Note: Debug/Verbose are filtered by Serilog's default MinimumLevel (Information)
                logger.LogInformation("info msg");
                logger.LogWarning("warning msg");
                logger.LogError(ex, "error msg");
                logger.LogCritical(ex, "critical msg");
            }

            _output.WriteLine($"[Restituito] Livelli=[{string.Join(", ", sink.Events.Select(e => e.Level))}], Eccezione[2]={sink.Events[2].Exception?.Message}");
            Assert.Equal(4, sink.Events.Count);
            Assert.Equal(LogEventLevel.Information, sink.Events[0].Level);
            Assert.Equal(LogEventLevel.Warning, sink.Events[1].Level);
            Assert.Equal(LogEventLevel.Error, sink.Events[2].Level);
            Assert.Equal(LogEventLevel.Fatal, sink.Events[3].Level);
            Assert.NotNull(sink.Events[2].Exception);
            Assert.Equal("test", sink.Events[2].Exception!.Message);
        }
    }

    // ── Trace (single-shot, template-based) ────────────────────────

    [Fact]
    public void Trace_LogsWithCorrectLevel() {
        _output.WriteLine("[Scenario] Trace(\"MyAction\", \"TXN-010\", LogLevel.Warning, null, \"test message\")");
        _output.WriteLine("[Atteso] Un solo evento con livello Serilog Warning");

        var (logger, sink, sp) = CreateCapturingLogger();
        using (sp) {
            logger.Trace("MyAction", "TXN-010", LogLevel.Warning, null, "test message");

            _output.WriteLine($"[Restituito] Eventi={sink.Events.Count}, Livello={sink.Events[0].Level}");
            Assert.Single(sink.Events);
            Assert.Equal(LogEventLevel.Warning, sink.Events[0].Level);
        }
    }

    [Fact]
    public void Trace_IncludesException() {
        _output.WriteLine("[Scenario] Trace livello Error con una ArgumentException(\"bad arg\")");
        _output.WriteLine("[Atteso] L'evento contiene l'eccezione con messaggio \"bad arg\"");

        var (logger, sink, sp) = CreateCapturingLogger();
        using (sp) {
            var ex = new ArgumentException("bad arg");
            logger.Trace("MyAction", "TXN-011", LogLevel.Error, ex, "error occurred");

            _output.WriteLine($"[Restituito] Eventi={sink.Events.Count}, Eccezione={sink.Events[0].Exception?.Message}");
            Assert.Single(sink.Events);
            Assert.NotNull(sink.Events[0].Exception);
            Assert.Equal("bad arg", sink.Events[0].Exception!.Message);
        }
    }

    [Fact]
    public void Trace_EnrichesWithIdTransactionAndAction() {
        _output.WriteLine("[Scenario] Trace(\"PaymentProcess\", \"TXN-012\", Information, null, \"payment ok\")");
        _output.WriteLine("[Atteso] Evento arricchito con IdTransaction (contiene TXN-012) e Action (contiene PaymentProcess)");

        var (logger, sink, sp) = CreateCapturingLogger();
        using (sp) {
            logger.Trace("PaymentProcess", "TXN-012", LogLevel.Information, null, "payment ok");

            Assert.Single(sink.Events);
            var evt = sink.Events[0];
            _output.WriteLine($"[Restituito] IdTransaction={evt.Properties.GetValueOrDefault("IdTransaction")}, Action={evt.Properties.GetValueOrDefault("Action")}");
            Assert.True(evt.Properties.ContainsKey("IdTransaction"));
            Assert.True(evt.Properties.ContainsKey("Action"));
            Assert.Contains("TXN-012", evt.Properties["IdTransaction"].ToString());
            Assert.Contains("PaymentProcess", evt.Properties["Action"].ToString());
        }
    }

    [Fact]
    public void Trace_Shorthand_UsesInformationLevel() {
        _output.WriteLine("[Scenario] Overload abbreviato Trace(\"MyAction\", \"TXN-013\", \"hello {Name}\", \"World\") senza livello esplicito");
        _output.WriteLine("[Atteso] Livello di default Information e proprietà Name valorizzata dal template");

        var (logger, sink, sp) = CreateCapturingLogger();
        using (sp) {
            logger.Trace("MyAction", "TXN-013", "hello {Name}", "World");

            _output.WriteLine($"[Restituito] Livello={sink.Events[0].Level}, HaName={sink.Events[0].Properties.ContainsKey("Name")}");
            Assert.Single(sink.Events);
            Assert.Equal(LogEventLevel.Information, sink.Events[0].Level);
            Assert.True(sink.Events[0].Properties.ContainsKey("Name"));
        }
    }

    [Fact]
    public void Trace_PreservesOriginalMessageArgs() {
        _output.WriteLine("[Scenario] Trace con template \"Order {OrderId} for {Customer}\" e argomenti 123 e \"Acme\"");
        _output.WriteLine("[Atteso] Gli argomenti del template restano come proprietà strutturate: OrderId contiene 123 e Customer contiene Acme");

        var (logger, sink, sp) = CreateCapturingLogger();
        using (sp) {
            logger.Trace("MyAction", "TXN-014", LogLevel.Information, null,
                "Order {OrderId} for {Customer}", 123, "Acme");

            Assert.Single(sink.Events);
            var evt = sink.Events[0];
            _output.WriteLine($"[Restituito] OrderId={evt.Properties.GetValueOrDefault("OrderId")}, Customer={evt.Properties.GetValueOrDefault("Customer")}");
            Assert.True(evt.Properties.ContainsKey("OrderId"));
            Assert.True(evt.Properties.ContainsKey("Customer"));
            Assert.Contains("123", evt.Properties["OrderId"].ToString());
            Assert.Contains("Acme", evt.Properties["Customer"].ToString());
        }
    }

    // ── Capturing sink ─────────────────────────────────────────────

    private sealed class CapturingSink : Serilog.Core.ILogEventSink {
        private readonly ConcurrentQueue<LogEvent> _events = new();
        public List<LogEvent> Events => [.. _events];

        public void Emit(LogEvent logEvent) {
            _events.Enqueue(logEvent);
        }
    }
}
