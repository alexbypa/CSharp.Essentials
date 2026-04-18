using System.Collections.Concurrent;
using System.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Serilog.Events;

namespace CSharpEssentials.LoggerHelper.Tests;

/// <summary>
/// Tests for TraceSync and TraceAsync extension methods.
/// Verifies parity with the original loggerExtension&lt;T&gt; behavior.
/// </summary>
public class LoggerExtensionsTests {
    /// <summary>
    /// Helper: builds a LoggerHelper ILogger backed by a capturing sink
    /// so we can inspect what was logged.
    /// </summary>
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

    // ── TraceSync ──────────────────────────────────────────────────

    [Fact]
    public void TraceSync_LogsWithCorrectLevel() {
        var (logger, sink, sp) = CreateCapturingLogger();
        using (sp) {
            logger.TraceSync("MyAction", "TXN-001", LogLevel.Warning, null, "test message");

            Assert.Single(sink.Events);
            Assert.Equal(LogEventLevel.Warning, sink.Events[0].Level);
        }
    }

    [Fact]
    public void TraceSync_IncludesException() {
        var (logger, sink, sp) = CreateCapturingLogger();
        using (sp) {
            var ex = new InvalidOperationException("boom");
            logger.TraceSync("MyAction", "TXN-002", LogLevel.Error, ex, "something failed");

            Assert.Single(sink.Events);
            Assert.Equal(LogEventLevel.Error, sink.Events[0].Level);
            Assert.NotNull(sink.Events[0].Exception);
            Assert.Equal("boom", sink.Events[0].Exception!.Message);
        }
    }

    [Fact]
    public void TraceSync_EnrichesWithIdTransactionAndAction() {
        var (logger, sink, sp) = CreateCapturingLogger();
        using (sp) {
            logger.TraceSync("OrderProcess", "TXN-100", LogLevel.Information, null, "order placed");

            Assert.Single(sink.Events);
            var evt = sink.Events[0];
            // Properties are appended to the message template as structured values
            Assert.True(evt.Properties.ContainsKey("IdTransaction"),
                "IdTransaction property missing from log event");
            Assert.True(evt.Properties.ContainsKey("Action"),
                "Action property missing from log event");
            Assert.Contains("TXN-100", evt.Properties["IdTransaction"].ToString());
            Assert.Contains("OrderProcess", evt.Properties["Action"].ToString());
        }
    }

    [Fact]
    public void TraceSync_Shorthand_UsesInformationLevel() {
        var (logger, sink, sp) = CreateCapturingLogger();
        using (sp) {
            logger.TraceSync("MyAction", "TXN-003", "hello {Name}", "World");

            Assert.Single(sink.Events);
            Assert.Equal(LogEventLevel.Information, sink.Events[0].Level);
        }
    }

    [Fact]
    public void TraceSync_EnrichesWithSpanName_WhenActivityIsActive() {
        var (logger, sink, sp) = CreateCapturingLogger();
        using (sp) {
            using var activity = new Activity("TestOperation").Start();

            logger.TraceSync("MyAction", "TXN-004", LogLevel.Information, null, "with span");

            Assert.Single(sink.Events);
            Assert.True(sink.Events[0].Properties.ContainsKey("SpanName"),
                "SpanName property missing when Activity is active");
            Assert.Contains("TestOperation", sink.Events[0].Properties["SpanName"].ToString());
        }
    }

    [Fact]
    public void TraceSync_SkipsSpanName_WhenNoActivityIsActive() {
        var (logger, sink, sp) = CreateCapturingLogger();
        using (sp) {
            // Make sure no activity is running
            Activity.Current?.Stop();

            logger.TraceSync("MyAction", "TXN-005", LogLevel.Information, null, "no span");

            Assert.Single(sink.Events);
            Assert.False(sink.Events[0].Properties.ContainsKey("SpanName"),
                "SpanName should not be present when no Activity is active");
        }
    }

    [Fact]
    public void TraceSync_SkipsWhenLevelDisabled() {
        var (logger, sink, sp) = CreateCapturingLogger();
        using (sp) {
            logger.TraceSync("MyAction", "TXN-006", LogLevel.Information, null, "should log");
            Assert.Single(sink.Events);
        }
    }

    [Fact]
    public void TraceSync_PreservesOriginalMessageArgs() {
        var (logger, sink, sp) = CreateCapturingLogger();
        using (sp) {
            logger.TraceSync("MyAction", "TXN-007", LogLevel.Information, null,
                "Order {OrderId} for {Customer}", 123, "Acme");

            Assert.Single(sink.Events);
            var evt = sink.Events[0];
            Assert.True(evt.Properties.ContainsKey("OrderId"));
            Assert.True(evt.Properties.ContainsKey("Customer"));
            Assert.Contains("123", evt.Properties["OrderId"].ToString());
            Assert.Contains("Acme", evt.Properties["Customer"].ToString());
        }
    }

    // ── TraceAsync ─────────────────────────────────────────────────

    [Fact]
    public void TraceAsync_DoesNotBlockCallingThread() {
        var (logger, sink, sp) = CreateCapturingLogger();
        using (sp) {
            var sw = Stopwatch.StartNew();

            // TraceAsync must return immediately (fire-and-forget)
            logger.TraceAsync("MyAction", "TXN-010", LogLevel.Information, null, "async fire and forget");

            sw.Stop();

            // The call itself should be nearly instant (< 50ms).
            // The actual log write happens on a background thread.
            Assert.True(sw.ElapsedMilliseconds < 50,
                $"TraceAsync blocked for {sw.ElapsedMilliseconds}ms — should return immediately");
        }
    }

    [Fact]
    public async Task TraceAsync_EventuallyWritesTheLog() {
        var (logger, sink, sp) = CreateCapturingLogger();
        using (sp) {
            logger.TraceAsync("MyAction", "TXN-011", LogLevel.Warning, null, "async message");

            // Give the background Task.Run time to complete
            await Task.Delay(500);

            Assert.Single(sink.Events);
            Assert.Equal(LogEventLevel.Warning, sink.Events[0].Level);
        }
    }

    [Fact]
    public async Task TraceAsync_IncludesException() {
        var (logger, sink, sp) = CreateCapturingLogger();
        using (sp) {
            var ex = new ArgumentException("bad arg");
            logger.TraceAsync("MyAction", "TXN-012", LogLevel.Error, ex, "async error");

            await Task.Delay(500);

            Assert.Single(sink.Events);
            Assert.NotNull(sink.Events[0].Exception);
            Assert.Equal("bad arg", sink.Events[0].Exception!.Message);
        }
    }

    [Fact]
    public async Task TraceAsync_EnrichesWithIdTransactionAndAction() {
        var (logger, sink, sp) = CreateCapturingLogger();
        using (sp) {
            logger.TraceAsync("PaymentProcess", "TXN-013", LogLevel.Information, null, "payment started");

            await Task.Delay(500);

            Assert.Single(sink.Events);
            var evt = sink.Events[0];
            Assert.True(evt.Properties.ContainsKey("IdTransaction"));
            Assert.True(evt.Properties.ContainsKey("Action"));
            Assert.Contains("TXN-013", evt.Properties["IdTransaction"].ToString());
            Assert.Contains("PaymentProcess", evt.Properties["Action"].ToString());
        }
    }

    [Fact]
    public async Task TraceAsync_Shorthand_UsesInformationLevel() {
        var (logger, sink, sp) = CreateCapturingLogger();
        using (sp) {
            logger.TraceAsync("MyAction", "TXN-014", "shorthand async {Value}", 42);

            await Task.Delay(500);

            Assert.Single(sink.Events);
            Assert.Equal(LogEventLevel.Information, sink.Events[0].Level);
        }
    }

    [Fact]
    public async Task TraceAsync_MultipleFireAndForget_AllEventuallyWritten() {
        var (logger, sink, sp) = CreateCapturingLogger();
        using (sp) {
            for (int i = 0; i < 10; i++)
                logger.TraceAsync("Batch", $"TXN-{i:D3}", LogLevel.Information, null, "msg {Index}", i);

            await Task.Delay(1000);

            Assert.Equal(10, sink.Events.Count);
        }
    }

    // ── Capturing sink ─────────────────────────────────────────────

    /// <summary>
    /// Thread-safe Serilog sink that captures events for test assertions.
    /// </summary>
    private sealed class CapturingSink : Serilog.Core.ILogEventSink {
        private readonly ConcurrentBag<LogEvent> _events = [];
        public List<LogEvent> Events => [.. _events];

        public void Emit(LogEvent logEvent) {
            _events.Add(logEvent);
        }
    }
}
