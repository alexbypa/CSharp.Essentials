using System.Collections.Concurrent;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Serilog.Core;
using Serilog.Events;
using Serilog.Parsing;

namespace CSharpEssentials.LoggerHelper.Tests;

/// <summary>
/// Tests for the built-in sensitive data masking enricher — both as a standalone
/// <see cref="ILogEventEnricher"/> and wired through the full LoggerHelper pipeline.
/// </summary>
public class SensitiveDataMaskingEnricherTests {
    private static LogEvent CreateEvent(params (string Name, object? Value)[] properties) {
        var template = new MessageTemplateParser().Parse("test message");
        var props = properties.Select(p => new LogEventProperty(p.Name, new ScalarValue(p.Value)));
        return new LogEvent(DateTimeOffset.UtcNow, LogEventLevel.Information, null, template, props);
    }

    private static string PropertyValue(LogEvent evt, string name) =>
        ((ScalarValue)evt.Properties[name]).Value as string ?? string.Empty;

    // ── Built-in presets ────────────────────────────────────────────

    [Fact]
    public void Email_Preset_MasksEmailAddress() {
        var options = new SensitiveDataMaskingOptions { Presets = ["Email"] };
        var enricher = new SensitiveDataMaskingEnricher(options);
        var evt = CreateEvent(("Message", "Login failed for alice@example.com"));

        enricher.Enrich(evt, new TestPropertyFactory());

        Assert.Equal("Login failed for ***MASKED***", PropertyValue(evt, "Message"));
    }

    [Fact]
    public void CreditCard_Preset_MasksCardNumber() {
        var options = new SensitiveDataMaskingOptions { Presets = ["CreditCard"] };
        var enricher = new SensitiveDataMaskingEnricher(options);
        var evt = CreateEvent(("CardNumber", "4532-1234-5678-9012"));

        enricher.Enrich(evt, new TestPropertyFactory());

        Assert.Equal("***MASKED***", PropertyValue(evt, "CardNumber"));
    }

    [Fact]
    public void BearerToken_Preset_MasksOnlyTokenKeepingPrefix() {
        var options = new SensitiveDataMaskingOptions { Presets = ["BearerToken"] };
        var enricher = new SensitiveDataMaskingEnricher(options);
        var evt = CreateEvent(("Header", "Authorization: Bearer abc123.def456"));

        enricher.Enrich(evt, new TestPropertyFactory());

        Assert.Equal("Authorization: Bearer ***MASKED***", PropertyValue(evt, "Header"));
    }

    [Fact]
    public void ConnectionStringSecret_Preset_MasksPasswordOnly() {
        var options = new SensitiveDataMaskingOptions { Presets = ["ConnectionStringSecret"] };
        var enricher = new SensitiveDataMaskingEnricher(options);
        var evt = CreateEvent(("ConnectionString", "Server=db;User Id=sa;Password=S3cr3t!;"));

        enricher.Enrich(evt, new TestPropertyFactory());

        Assert.Equal("Server=db;User Id=sa;Password=***MASKED***;", PropertyValue(evt, "ConnectionString"));
    }

    // ── Sensitive property names ────────────────────────────────────

    [Fact]
    public void SensitiveProperty_ReplacesValueRegardlessOfContent() {
        var options = new SensitiveDataMaskingOptions { SensitiveProperties = ["Password"] };
        var enricher = new SensitiveDataMaskingEnricher(options);
        var evt = CreateEvent(("Password", "anything-goes-here"), ("UserName", "alice"));

        enricher.Enrich(evt, new TestPropertyFactory());

        Assert.Equal("***MASKED***", PropertyValue(evt, "Password"));
        Assert.Equal("alice", PropertyValue(evt, "UserName"));
    }

    [Fact]
    public void SensitiveProperty_IsCaseInsensitive() {
        var options = new SensitiveDataMaskingOptions { SensitiveProperties = ["apikey"] };
        var enricher = new SensitiveDataMaskingEnricher(options);
        var evt = CreateEvent(("ApiKey", "sk-live-12345"));

        enricher.Enrich(evt, new TestPropertyFactory());

        Assert.Equal("***MASKED***", PropertyValue(evt, "ApiKey"));
    }

    // ── Custom rules + custom mask text ─────────────────────────────

    [Fact]
    public void CustomRule_MasksWithCustomMaskText() {
        var options = new SensitiveDataMaskingOptions {
            MaskText = "[REDACTED]",
            Rules = [new MaskingRule { Name = "OrderId", Pattern = @"ORD-\d+" }]
        };
        var enricher = new SensitiveDataMaskingEnricher(options);
        var evt = CreateEvent(("Message", "Refund issued for ORD-99821"));

        enricher.Enrich(evt, new TestPropertyFactory());

        Assert.Equal("Refund issued for [REDACTED]", PropertyValue(evt, "Message"));
    }

    [Fact]
    public void Disabled_NoPresetsNoRulesNoSensitiveProperties_LeavesEventUnchanged() {
        var options = new SensitiveDataMaskingOptions();
        var enricher = new SensitiveDataMaskingEnricher(options);
        var evt = CreateEvent(("Message", "alice@example.com"));

        enricher.Enrich(evt, new TestPropertyFactory());

        Assert.Equal("alice@example.com", PropertyValue(evt, "Message"));
    }

    [Fact]
    public void RenderedMessage_IsMaskedWhenPresent() {
        var options = new SensitiveDataMaskingOptions { Presets = ["Email"] };
        var enricher = new SensitiveDataMaskingEnricher(options);
        var evt = CreateEvent(("RenderedMessage", "Welcome alice@example.com"));

        enricher.Enrich(evt, new TestPropertyFactory());

        Assert.Equal("Welcome ***MASKED***", PropertyValue(evt, "RenderedMessage"));
    }

    // ── Fluent builder ───────────────────────────────────────────────

    [Fact]
    public void EnableSensitiveDataMasking_SetsEnabledFlag() {
        var builder = new LoggerHelperBuilder();
        builder.EnableSensitiveDataMasking(o => o.Presets.Add("Email"));

        Assert.True(builder.Options.SensitiveDataMasking.Enabled);
        Assert.Contains("Email", builder.Options.SensitiveDataMasking.Presets);
    }

    // ── End-to-end through the LoggerHelper pipeline ────────────────

    [Fact]
    public void Pipeline_WithMaskingEnabled_RedactsEmailInStructuredProperty() {
        var sink = new CapturingSink();
        var services = new ServiceCollection();
        services.AddLoggerHelper(b => b
            .WithApplicationName("TestApp")
            .DisableOpenTelemetry()
            .AddRoute("Null", LogEventLevel.Information)
            .EnableSensitiveDataMasking(o => {
                o.Presets.Add("Email");
                o.SensitiveProperties.Add("Password");
            })
            .WithEnrichers(lc => lc.WriteTo.Sink(sink)));

        using var sp = services.BuildServiceProvider();
        var logger = sp.GetRequiredService<ILoggerProvider>().CreateLogger("Test");

        logger.LogInformation("Login attempt for {Email} with {Password}", "alice@example.com", "Sup3rSecret!");

        var evt = Assert.Single(sink.Events);
        Assert.Equal("***MASKED***", PropertyValue(evt, "Email"));
        Assert.Equal("***MASKED***", PropertyValue(evt, "Password"));
    }

    [Fact]
    public void Pipeline_WithMaskingDisabled_LeavesValuesIntact() {
        var sink = new CapturingSink();
        var services = new ServiceCollection();
        services.AddLoggerHelper(b => b
            .WithApplicationName("TestApp")
            .DisableOpenTelemetry()
            .AddRoute("Null", LogEventLevel.Information)
            .WithEnrichers(lc => lc.WriteTo.Sink(sink)));

        using var sp = services.BuildServiceProvider();
        var logger = sp.GetRequiredService<ILoggerProvider>().CreateLogger("Test");

        logger.LogInformation("Login attempt for {Email}", "alice@example.com");

        var evt = Assert.Single(sink.Events);
        Assert.Equal("alice@example.com", PropertyValue(evt, "Email"));
    }

    private sealed class TestPropertyFactory : ILogEventPropertyFactory {
        public LogEventProperty CreateProperty(string name, object? value, bool destructureObjects = false) =>
            new(name, new ScalarValue(value));
    }

    private sealed class CapturingSink : ILogEventSink {
        private readonly ConcurrentQueue<LogEvent> _events = new();
        public List<LogEvent> Events => [.. _events];

        public void Emit(LogEvent logEvent) => _events.Enqueue(logEvent);
    }
}
