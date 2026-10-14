using Xunit.Abstractions;
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
    private readonly ITestOutputHelper _output;

    public SensitiveDataMaskingEnricherTests(ITestOutputHelper output) {
        _output = output;
    }

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
        _output.WriteLine("[Scenario] Preset \"Email\" attivo: proprietà Message = \"Login failed for alice@example.com\"");
        _output.WriteLine("[Atteso] L'indirizzo email viene mascherato: \"Login failed for ***MASKED***\"");

        var options = new SensitiveDataMaskingOptions { Presets = ["Email"] };
        var enricher = new SensitiveDataMaskingEnricher(options);
        var evt = CreateEvent(("Message", "Login failed for alice@example.com"));

        enricher.Enrich(evt, new TestPropertyFactory());

        _output.WriteLine($"[Restituito] Message=\"{PropertyValue(evt, "Message")}\"");
        Assert.Equal("Login failed for ***MASKED***", PropertyValue(evt, "Message"));
    }

    [Fact]
    public void CreditCard_Preset_MasksCardNumber() {
        _output.WriteLine("[Scenario] Preset \"CreditCard\" attivo: proprietà CardNumber = \"4532-1234-5678-9012\"");
        _output.WriteLine("[Atteso] Il numero di carta viene mascherato: \"***MASKED***\"");

        var options = new SensitiveDataMaskingOptions { Presets = ["CreditCard"] };
        var enricher = new SensitiveDataMaskingEnricher(options);
        var evt = CreateEvent(("CardNumber", "4532-1234-5678-9012"));

        enricher.Enrich(evt, new TestPropertyFactory());

        _output.WriteLine($"[Restituito] CardNumber=\"{PropertyValue(evt, "CardNumber")}\"");
        Assert.Equal("***MASKED***", PropertyValue(evt, "CardNumber"));
    }

    [Fact]
    public void BearerToken_Preset_MasksOnlyTokenKeepingPrefix() {
        _output.WriteLine("[Scenario] Preset \"BearerToken\" attivo: proprietà Header = \"Authorization: Bearer abc123.def456\"");
        _output.WriteLine("[Atteso] Si maschera solo il token e si mantiene il prefisso: \"Authorization: Bearer ***MASKED***\"");

        var options = new SensitiveDataMaskingOptions { Presets = ["BearerToken"] };
        var enricher = new SensitiveDataMaskingEnricher(options);
        var evt = CreateEvent(("Header", "Authorization: Bearer abc123.def456"));

        enricher.Enrich(evt, new TestPropertyFactory());

        _output.WriteLine($"[Restituito] Header=\"{PropertyValue(evt, "Header")}\"");
        Assert.Equal("Authorization: Bearer ***MASKED***", PropertyValue(evt, "Header"));
    }

    [Fact]
    public void ConnectionStringSecret_Preset_MasksPasswordOnly() {
        _output.WriteLine("[Scenario] Preset \"ConnectionStringSecret\" attivo: connection string \"Server=db;User Id=sa;Password=S3cr3t!;\"");
        _output.WriteLine("[Atteso] Si maschera solo la password: \"Server=db;User Id=sa;Password=***MASKED***;\"");

        var options = new SensitiveDataMaskingOptions { Presets = ["ConnectionStringSecret"] };
        var enricher = new SensitiveDataMaskingEnricher(options);
        var evt = CreateEvent(("ConnectionString", "Server=db;User Id=sa;Password=S3cr3t!;"));

        enricher.Enrich(evt, new TestPropertyFactory());

        _output.WriteLine($"[Restituito] ConnectionString=\"{PropertyValue(evt, "ConnectionString")}\"");
        Assert.Equal("Server=db;User Id=sa;Password=***MASKED***;", PropertyValue(evt, "ConnectionString"));
    }

    // ── Sensitive property names ────────────────────────────────────

    [Fact]
    public void SensitiveProperty_ReplacesValueRegardlessOfContent() {
        _output.WriteLine("[Scenario] SensitiveProperties = [\"Password\"]: evento con Password=\"anything-goes-here\" e UserName=\"alice\"");
        _output.WriteLine("[Atteso] Password sostituita sempre con \"***MASKED***\" a prescindere dal contenuto; UserName resta \"alice\"");

        var options = new SensitiveDataMaskingOptions { SensitiveProperties = ["Password"] };
        var enricher = new SensitiveDataMaskingEnricher(options);
        var evt = CreateEvent(("Password", "anything-goes-here"), ("UserName", "alice"));

        enricher.Enrich(evt, new TestPropertyFactory());

        _output.WriteLine($"[Restituito] Password=\"{PropertyValue(evt, "Password")}\", UserName=\"{PropertyValue(evt, "UserName")}\"");
        Assert.Equal("***MASKED***", PropertyValue(evt, "Password"));
        Assert.Equal("alice", PropertyValue(evt, "UserName"));
    }

    [Fact]
    public void SensitiveProperty_IsCaseInsensitive() {
        _output.WriteLine("[Scenario] SensitiveProperties = [\"apikey\"] (minuscolo) ed evento con proprietà \"ApiKey\" (maiuscole diverse)");
        _output.WriteLine("[Atteso] Il confronto del nome è case-insensitive: ApiKey = \"***MASKED***\"");

        var options = new SensitiveDataMaskingOptions { SensitiveProperties = ["apikey"] };
        var enricher = new SensitiveDataMaskingEnricher(options);
        var evt = CreateEvent(("ApiKey", "sk-live-12345"));

        enricher.Enrich(evt, new TestPropertyFactory());

        _output.WriteLine($"[Restituito] ApiKey=\"{PropertyValue(evt, "ApiKey")}\"");
        Assert.Equal("***MASKED***", PropertyValue(evt, "ApiKey"));
    }

    // ── Custom rules + custom mask text ─────────────────────────────

    [Fact]
    public void CustomRule_MasksWithCustomMaskText() {
        _output.WriteLine("[Scenario] Regola custom (pattern ORD-\\d+) con MaskText=\"[REDACTED]\" su \"Refund issued for ORD-99821\"");
        _output.WriteLine("[Atteso] Il match viene sostituito col testo personalizzato: \"Refund issued for [REDACTED]\"");

        var options = new SensitiveDataMaskingOptions {
            MaskText = "[REDACTED]",
            Rules = [new MaskingRule { Name = "OrderId", Pattern = @"ORD-\d+" }]
        };
        var enricher = new SensitiveDataMaskingEnricher(options);
        var evt = CreateEvent(("Message", "Refund issued for ORD-99821"));

        enricher.Enrich(evt, new TestPropertyFactory());

        _output.WriteLine($"[Restituito] Message=\"{PropertyValue(evt, "Message")}\"");
        Assert.Equal("Refund issued for [REDACTED]", PropertyValue(evt, "Message"));
    }

    [Fact]
    public void Disabled_NoPresetsNoRulesNoSensitiveProperties_LeavesEventUnchanged() {
        _output.WriteLine("[Scenario] Opzioni vuote (nessun preset, nessuna regola, nessuna property sensibile) su Message=\"alice@example.com\"");
        _output.WriteLine("[Atteso] L'evento resta invariato: \"alice@example.com\" (masking inattivo)");

        var options = new SensitiveDataMaskingOptions();
        var enricher = new SensitiveDataMaskingEnricher(options);
        var evt = CreateEvent(("Message", "alice@example.com"));

        enricher.Enrich(evt, new TestPropertyFactory());

        _output.WriteLine($"[Restituito] Message=\"{PropertyValue(evt, "Message")}\"");
        Assert.Equal("alice@example.com", PropertyValue(evt, "Message"));
    }

    [Fact]
    public void RenderedMessage_IsMaskedWhenPresent() {
        _output.WriteLine("[Scenario] Preset \"Email\" attivo: proprietà RenderedMessage = \"Welcome alice@example.com\"");
        _output.WriteLine("[Atteso] Anche RenderedMessage viene mascherato: \"Welcome ***MASKED***\"");

        var options = new SensitiveDataMaskingOptions { Presets = ["Email"] };
        var enricher = new SensitiveDataMaskingEnricher(options);
        var evt = CreateEvent(("RenderedMessage", "Welcome alice@example.com"));

        enricher.Enrich(evt, new TestPropertyFactory());

        _output.WriteLine($"[Restituito] RenderedMessage=\"{PropertyValue(evt, "RenderedMessage")}\"");
        Assert.Equal("Welcome ***MASKED***", PropertyValue(evt, "RenderedMessage"));
    }

    [Fact]
    public void RenderedMessage_IsReRenderedWhenSensitivePropertyIsMasked() {
        _output.WriteLine("[Scenario] Template \"Login {User} with {Password}\", SensitiveProperties = [Password]; RenderedMessageEnricher gira PRIMA del masking (ordine della pipeline)");
        _output.WriteLine("[Atteso] RenderedMessage non contiene più la password in chiaro: \"Login \\\"bob\\\" with \\\"***MASKED***\\\"\"");

        var template = new MessageTemplateParser().Parse("Login {User} with {Password}");
        var evt = new LogEvent(DateTimeOffset.UtcNow, LogEventLevel.Information, null, template, [
            new LogEventProperty("User", new ScalarValue("bob")),
            new LogEventProperty("Password", new ScalarValue("Sup3rSecret!"))
        ]);
        var factory = new TestPropertyFactory();
        new RenderedMessageEnricher().Enrich(evt, factory);

        new SensitiveDataMaskingEnricher(new SensitiveDataMaskingOptions { SensitiveProperties = ["Password"] })
            .Enrich(evt, factory);

        var rendered = PropertyValue(evt, "RenderedMessage");
        _output.WriteLine($"[Restituito] RenderedMessage=\"{rendered}\"");
        Assert.DoesNotContain("Sup3rSecret!", rendered);
        Assert.Equal("Login \"bob\" with \"***MASKED***\"", rendered);
    }

    // ── ReDoS protection ─────────────────────────────────────────────

    [Fact]
    public void CatastrophicRule_TimesOut_AndMasksWholeValue() {
        _output.WriteLine("[Scenario] Regola custom (a+)+$ (backtracking catastrofico) su 40 'a' seguite da '!'");
        _output.WriteLine("[Atteso] Il match va in timeout: valore interamente mascherato, nessun blocco del thread");

        var options = new SensitiveDataMaskingOptions {
            Rules = [new MaskingRule { Name = "Evil", Pattern = @"(a+)+$" }]
        };
        var enricher = new SensitiveDataMaskingEnricher(options);
        var evt = CreateEvent(("Message", new string('a', 40) + "!"));

        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        enricher.Enrich(evt, new TestPropertyFactory());
        stopwatch.Stop();

        _output.WriteLine($"[Restituito] Message=\"{PropertyValue(evt, "Message")}\" in {stopwatch.ElapsedMilliseconds} ms");
        Assert.Equal("***MASKED***", PropertyValue(evt, "Message"));
        Assert.True(stopwatch.Elapsed < TimeSpan.FromSeconds(5));
    }

    // ── Fluent builder ───────────────────────────────────────────────

    [Fact]
    public void EnableSensitiveDataMasking_SetsEnabledFlag() {
        _output.WriteLine("[Scenario] LoggerHelperBuilder.EnableSensitiveDataMasking(o => o.Presets.Add(\"Email\"))");
        _output.WriteLine("[Atteso] SensitiveDataMasking.Enabled == true e il preset \"Email\" è presente");

        var builder = new LoggerHelperBuilder();
        builder.EnableSensitiveDataMasking(o => o.Presets.Add("Email"));

        _output.WriteLine($"[Restituito] Enabled={builder.Options.SensitiveDataMasking.Enabled}, Presets=[{string.Join(", ", builder.Options.SensitiveDataMasking.Presets)}]");
        Assert.True(builder.Options.SensitiveDataMasking.Enabled);
        Assert.Contains("Email", builder.Options.SensitiveDataMasking.Presets);
    }

    // ── End-to-end through the LoggerHelper pipeline ────────────────

    [Fact]
    public void Pipeline_WithMaskingEnabled_RedactsEmailInStructuredProperty() {
        _output.WriteLine("[Scenario] Pipeline completa con masking attivo (preset Email + property Password): LogInformation(\"Login attempt for {Email} with {Password}\") con alice@example.com e Sup3rSecret!");
        _output.WriteLine("[Atteso] Le proprietà strutturate Email e Password arrivano al sink già mascherate (\"***MASKED***\")");

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
        _output.WriteLine($"[Restituito] Email=\"{PropertyValue(evt, "Email")}\", Password=\"{PropertyValue(evt, "Password")}\"");
        Assert.Equal("***MASKED***", PropertyValue(evt, "Email"));
        Assert.Equal("***MASKED***", PropertyValue(evt, "Password"));
    }

    [Fact]
    public void Pipeline_WithMaskingDisabled_LeavesValuesIntact() {
        _output.WriteLine("[Scenario] Pipeline completa senza masking: LogInformation(\"Login attempt for {Email}\") con alice@example.com");
        _output.WriteLine("[Atteso] La proprietà Email arriva al sink intatta (\"alice@example.com\")");

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
        _output.WriteLine($"[Restituito] Email=\"{PropertyValue(evt, "Email")}\"");
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
