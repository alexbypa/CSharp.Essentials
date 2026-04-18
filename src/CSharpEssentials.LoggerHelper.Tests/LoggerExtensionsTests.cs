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
        var (logger, sink, sp) = CreateCapturingLogger();
        using (sp) {
            using (logger.BeginTrace("OrderProcess", "TXN-100")) {
                logger.LogInformation("Step 1: validate");
                logger.LogWarning("Step 2: low stock");
                logger.LogError("Step 3: payment failed");
            }

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
        var (logger, sink, sp) = CreateCapturingLogger();
        using (sp) {
            using (logger.BeginTrace("Inside", "TXN-IN")) {
                logger.LogInformation("inside scope");
            }
            logger.LogInformation("outside scope");

            Assert.Equal(2, sink.Events.Count);
            // First log (inside) has enrichment
            Assert.True(sink.Events[0].Properties.ContainsKey("IdTransaction"));
            // Second log (outside) does NOT
            Assert.False(sink.Events[1].Properties.ContainsKey("IdTransaction"));
        }
    }

    [Fact]
    public void BeginTrace_IncludesSpanNameWhenActivityIsActive() {
        var (logger, sink, sp) = CreateCapturingLogger();
        using (sp) {
            using var activity = new Activity("TestOp").Start();
            using (logger.BeginTrace("MyAction", "TXN-001")) {
                logger.LogInformation("with span");
            }

            Assert.Single(sink.Events);
            Assert.True(sink.Events[0].Properties.ContainsKey("SpanName"));
            Assert.Contains("TestOp", sink.Events[0].Properties["SpanName"].ToString());
        }
    }

    [Fact]
    public void BeginTrace_NoSpanNameWhenNoActivity() {
        var (logger, sink, sp) = CreateCapturingLogger();
        using (sp) {
            Activity.Current?.Stop();
            using (logger.BeginTrace("MyAction", "TXN-002")) {
                logger.LogInformation("no span");
            }

            Assert.Single(sink.Events);
            Assert.False(sink.Events[0].Properties.ContainsKey("SpanName"));
        }
    }

    [Fact]
    public void BeginTrace_WorksWithAllLogLevels() {
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
        var (logger, sink, sp) = CreateCapturingLogger();
        using (sp) {
            logger.Trace("MyAction", "TXN-010", LogLevel.Warning, null, "test message");

            Assert.Single(sink.Events);
            Assert.Equal(LogEventLevel.Warning, sink.Events[0].Level);
        }
    }

    [Fact]
    public void Trace_IncludesException() {
        var (logger, sink, sp) = CreateCapturingLogger();
        using (sp) {
            var ex = new ArgumentException("bad arg");
            logger.Trace("MyAction", "TXN-011", LogLevel.Error, ex, "error occurred");

            Assert.Single(sink.Events);
            Assert.NotNull(sink.Events[0].Exception);
            Assert.Equal("bad arg", sink.Events[0].Exception!.Message);
        }
    }

    [Fact]
    public void Trace_EnrichesWithIdTransactionAndAction() {
        var (logger, sink, sp) = CreateCapturingLogger();
        using (sp) {
            logger.Trace("PaymentProcess", "TXN-012", LogLevel.Information, null, "payment ok");

            Assert.Single(sink.Events);
            var evt = sink.Events[0];
            Assert.True(evt.Properties.ContainsKey("IdTransaction"));
            Assert.True(evt.Properties.ContainsKey("Action"));
            Assert.Contains("TXN-012", evt.Properties["IdTransaction"].ToString());
            Assert.Contains("PaymentProcess", evt.Properties["Action"].ToString());
        }
    }

    [Fact]
    public void Trace_Shorthand_UsesInformationLevel() {
        var (logger, sink, sp) = CreateCapturingLogger();
        using (sp) {
            logger.Trace("MyAction", "TXN-013", "hello {Name}", "World");

            Assert.Single(sink.Events);
            Assert.Equal(LogEventLevel.Information, sink.Events[0].Level);
            Assert.True(sink.Events[0].Properties.ContainsKey("Name"));
        }
    }

    [Fact]
    public void Trace_PreservesOriginalMessageArgs() {
        var (logger, sink, sp) = CreateCapturingLogger();
        using (sp) {
            logger.Trace("MyAction", "TXN-014", LogLevel.Information, null,
                "Order {OrderId} for {Customer}", 123, "Acme");

            Assert.Single(sink.Events);
            var evt = sink.Events[0];
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
