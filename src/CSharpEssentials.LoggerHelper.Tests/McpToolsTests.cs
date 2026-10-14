using Xunit.Abstractions;
using CSharpEssentials.LoggerHelper.Diagnostics;
using CSharpEssentials.LoggerHelper.MCP;

namespace CSharpEssentials.LoggerHelper.Tests;

/// <summary>
/// Unit tests for <see cref="LoggerHelperMcpTools"/> — v5.0.9 killer feature.
/// Each test verifies one of the four MCP tools without any ASP.NET Core infrastructure.
/// </summary>
public class McpToolsTests {
    private readonly ITestOutputHelper _output;

    public McpToolsTests(ITestOutputHelper output) {
        _output = output;
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    private static LoggerHelperMcpTools Build(
        LogErrorStore?       errorStore = null,
        LoadedSinkStore?     sinkStore  = null,
        LoggerHelperOptions? options    = null) =>
        new(
            errorStore ?? new LogErrorStore(),
            sinkStore  ?? new LoadedSinkStore(),
            options    ?? new LoggerHelperOptions { ApplicationName = "TestApp" }
        );

    // ── loggerhelper_get_health ───────────────────────────────────────────────

    [Fact]
    public void GetHealth_NoErrors_ReturnsOk() {
        _output.WriteLine("[Scenario] Tool MCP get_health con LogErrorStore vuoto");
        _output.WriteLine("[Atteso] Il report indica stato \"OK\"");

        var result = Build().GetHealth();
        _output.WriteLine($"[Restituito] Report=\"{result}\"");
        Assert.Contains("OK", result);
    }

    [Fact]
    public void GetHealth_FewErrors_ReturnsWarning() {
        _output.WriteLine("[Scenario] Tool MCP get_health con 5 errori di sink Email registrati");
        _output.WriteLine("[Atteso] Il report indica stato \"WARNING\" (pochi errori)");

        var store = new LogErrorStore();
        for (int i = 0; i < 5; i++)
            store.Add(new LogErrorEntry { SinkName = "Email", ErrorMessage = $"Timeout {i}" });

        var result = Build(errorStore: store).GetHealth();
        _output.WriteLine($"[Restituito] Report=\"{result}\"");
        Assert.Contains("WARNING", result);
    }

    [Fact]
    public void GetHealth_ManyErrors_ReturnsCritical() {
        _output.WriteLine("[Scenario] Tool MCP get_health con 12 errori di sink Telegram registrati");
        _output.WriteLine("[Atteso] Il report indica stato \"CRITICAL\" (molti errori)");

        var store = new LogErrorStore();
        for (int i = 0; i < 12; i++)
            store.Add(new LogErrorEntry { SinkName = "Telegram", ErrorMessage = $"Error {i}" });

        var result = Build(errorStore: store).GetHealth();
        _output.WriteLine($"[Restituito] Report=\"{result}\"");
        Assert.Contains("CRITICAL", result);
    }

    [Fact]
    public void GetHealth_IncludesErrorCount() {
        _output.WriteLine("[Scenario] Tool MCP get_health con 1 errore registrato (sink File, \"Disk full\")");
        _output.WriteLine("[Atteso] Il report include il numero di errori (\"1 recorded\")");

        var store = new LogErrorStore();
        store.Add(new LogErrorEntry { SinkName = "File", ErrorMessage = "Disk full" });

        var result = Build(errorStore: store).GetHealth();
        _output.WriteLine($"[Restituito] Report=\"{result}\"");
        Assert.Contains("1 recorded", result);
    }

    // ── loggerhelper_get_errors ───────────────────────────────────────────────

    [Fact]
    public void GetErrors_Empty_ReturnsNoErrors() {
        _output.WriteLine("[Scenario] Tool MCP get_errors con LogErrorStore vuoto");
        _output.WriteLine("[Atteso] Messaggio \"No sink errors\"");

        var result = Build().GetErrors();
        _output.WriteLine($"[Restituito] Risposta=\"{result}\"");
        Assert.Contains("No sink errors", result);
    }

    [Fact]
    public void GetErrors_WithErrors_ShowsSinkNameAndMessage() {
        _output.WriteLine("[Scenario] Tool MCP get_errors con un errore (sink MSSqlServer, \"Connection refused\")");
        _output.WriteLine("[Atteso] La risposta mostra sia il nome del sink sia il messaggio di errore");

        var store = new LogErrorStore();
        store.Add(new LogErrorEntry { SinkName = "MSSqlServer", ErrorMessage = "Connection refused" });

        var result = Build(errorStore: store).GetErrors();
        _output.WriteLine($"[Restituito] Risposta=\"{result}\"");
        Assert.Contains("MSSqlServer", result);
        Assert.Contains("Connection refused", result);
    }

    [Fact]
    public void GetErrors_CountParameter_LimitsResults() {
        _output.WriteLine("[Scenario] Tool MCP get_errors(count: 5) con 30 errori registrati");
        _output.WriteLine("[Atteso] L'intestazione indica \"5 of 30\": la risposta è limitata a 5 elementi sul totale di 30");

        var store = new LogErrorStore();
        for (int i = 0; i < 30; i++)
            store.Add(new LogErrorEntry { SinkName = "Seq", ErrorMessage = $"Error #{i:D2}" });

        var result = Build(errorStore: store).GetErrors(count: 5);
        _output.WriteLine($"[Restituito] Risposta=\"{result}\"");
        // 30 total, 5 requested — header should say "5 of 30"
        Assert.Contains("5 of 30", result);
    }

    [Fact]
    public void GetErrors_WithContextInfo_ShowsContext() {
        _output.WriteLine("[Scenario] Tool MCP get_errors con un errore (Elasticsearch, \"Index not found\") che ha ContextInfo \"index=logs-2026\"");
        _output.WriteLine("[Atteso] La risposta include anche il contesto \"logs-2026\"");

        var store = new LogErrorStore();
        store.Add(new LogErrorEntry {
            SinkName    = "Elasticsearch",
            ErrorMessage = "Index not found",
            ContextInfo  = "index=logs-2026"
        });

        var result = Build(errorStore: store).GetErrors();
        _output.WriteLine($"[Restituito] Risposta=\"{result}\"");
        Assert.Contains("logs-2026", result);
    }

    // ── loggerhelper_get_sinks ────────────────────────────────────────────────

    [Fact]
    public void GetSinks_Empty_ReturnsNoSinks() {
        _output.WriteLine("[Scenario] Tool MCP get_sinks con LoadedSinkStore vuoto");
        _output.WriteLine("[Atteso] Messaggio \"No sinks\"");

        var result = Build().GetSinks();
        _output.WriteLine($"[Restituito] Risposta=\"{result}\"");
        Assert.Contains("No sinks", result);
    }

    [Fact]
    public void GetSinks_WithActiveSink_ShowsActive() {
        _output.WriteLine("[Scenario] Tool MCP get_sinks con sink Console caricato e Configured=true");
        _output.WriteLine("[Atteso] La risposta mostra \"ACTIVE\" e il nome \"Console\"");

        var store = new LoadedSinkStore();
        store.Add(new LoadedSinkInfo {
            SinkName   = "Console",
            PluginType = "ConsoleSinkPlugin",
            Levels     = ["Information", "Warning"],
            Configured = true
        });

        var result = Build(sinkStore: store).GetSinks();
        _output.WriteLine($"[Restituito] Risposta=\"{result}\"");
        Assert.Contains("ACTIVE", result);
        Assert.Contains("Console", result);
    }

    [Fact]
    public void GetSinks_WithFailedSink_ShowsFailed() {
        _output.WriteLine("[Scenario] Tool MCP get_sinks con sink MSSqlServer caricato ma Configured=false");
        _output.WriteLine("[Atteso] La risposta mostra \"FAILED\" e il nome \"MSSqlServer\"");

        var store = new LoadedSinkStore();
        store.Add(new LoadedSinkInfo {
            SinkName   = "MSSqlServer",
            PluginType = "MSSqlServerSinkPlugin",
            Levels     = ["Error"],
            Configured = false
        });

        var result = Build(sinkStore: store).GetSinks();
        _output.WriteLine($"[Restituito] Risposta=\"{result}\"");
        Assert.Contains("FAILED", result);
        Assert.Contains("MSSqlServer", result);
    }

    // ── loggerhelper_get_config ───────────────────────────────────────────────

    [Fact]
    public void GetConfig_ReturnsApplicationName() {
        _output.WriteLine("[Scenario] Tool MCP get_config con ApplicationName=\"MyProductionApp\"");
        _output.WriteLine("[Atteso] La risposta contiene il nome applicazione");

        var options = new LoggerHelperOptions { ApplicationName = "MyProductionApp" };
        var result  = Build(options: options).GetConfig();
        _output.WriteLine($"[Restituito] Risposta=\"{result}\"");
        Assert.Contains("MyProductionApp", result);
    }

    [Fact]
    public void GetConfig_MaskingEnabled_ShowsEnabled() {
        _output.WriteLine("[Scenario] Tool MCP get_config con SensitiveDataMasking.Enabled=true");
        _output.WriteLine("[Atteso] La risposta indica che il masking è \"enabled\"");

        var options = new LoggerHelperOptions {
            ApplicationName      = "App",
            SensitiveDataMasking = new SensitiveDataMaskingOptions { Enabled = true }
        };
        var result = Build(options: options).GetConfig();
        _output.WriteLine($"[Restituito] Risposta=\"{result}\"");
        Assert.Contains("enabled", result);
    }

    [Fact]
    public void GetConfig_MaskingDisabled_ShowsDisabled() {
        _output.WriteLine("[Scenario] Tool MCP get_config con SensitiveDataMasking.Enabled=false");
        _output.WriteLine("[Atteso] La risposta indica che il masking è \"disabled\"");

        var options = new LoggerHelperOptions {
            ApplicationName      = "App",
            SensitiveDataMasking = new SensitiveDataMaskingOptions { Enabled = false }
        };
        var result = Build(options: options).GetConfig();
        _output.WriteLine($"[Restituito] Risposta=\"{result}\"");
        Assert.Contains("disabled", result);
    }

    [Fact]
    public void GetConfig_WithRoutes_ShowsRoutes() {
        _output.WriteLine("[Scenario] Tool MCP get_config con 2 route configurate (Console: Information/Error, File: Error/Fatal)");
        _output.WriteLine("[Atteso] La risposta elenca i sink Console e File e riporta \"Routes (2)\"");

        var options = new LoggerHelperOptions {
            ApplicationName = "App",
            Routes = [
                new SinkRouting { Sink = "Console", Levels = ["Information", "Error"] },
                new SinkRouting { Sink = "File",    Levels = ["Error", "Fatal"] }
            ]
        };
        var result = Build(options: options).GetConfig();
        _output.WriteLine($"[Restituito] Risposta=\"{result}\"");
        Assert.Contains("Console", result);
        Assert.Contains("File", result);
        Assert.Contains("Routes (2)", result);
    }
}
