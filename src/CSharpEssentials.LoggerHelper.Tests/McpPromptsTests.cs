using Xunit.Abstractions;
using CSharpEssentials.LoggerHelper.MCP;
using System.Text.Json;

namespace CSharpEssentials.LoggerHelper.Tests;

/// <summary>
/// Unit tests for <see cref="LoggerHelperMcpPrompts"/> — MCP Espansione (prompts/list, prompts/get).
/// </summary>
public class McpPromptsTests {
    private readonly ITestOutputHelper _output;

    public McpPromptsTests(ITestOutputHelper output) {
        _output = output;
    }

    // ── prompts/list ──────────────────────────────────────────────────────────

    [Fact]
    public void BuildList_ReturnsAtLeastOnePrompt() {
        _output.WriteLine("[Scenario] MCP prompts/list: chiamo LoggerHelperMcpPrompts.BuildList()");
        _output.WriteLine("[Atteso] La lista contiene almeno un prompt");

        var prompts = LoggerHelperMcpPrompts.BuildList();
        _output.WriteLine($"[Restituito] Prompts={prompts.Count()}");
        Assert.NotEmpty(prompts);
    }

    [Fact]
    public void BuildList_ContainsDiagnoseLogging() {
        _output.WriteLine("[Scenario] MCP prompts/list: cerco il prompt \"diagnose-logging\" nella lista");
        _output.WriteLine("[Atteso] Esiste un prompt con Name == \"diagnose-logging\"");

        var prompts = LoggerHelperMcpPrompts.BuildList();
        _output.WriteLine($"[Restituito] Prompts=[{string.Join(", ", prompts.Select(p => p.Name))}]");
        Assert.Contains(prompts, p => p.Name == "diagnose-logging");
    }

    [Fact]
    public void BuildList_DiagnoseLogging_HasFocusArgument() {
        _output.WriteLine("[Scenario] MCP prompts/list: ispeziono gli argomenti del prompt \"diagnose-logging\"");
        _output.WriteLine("[Atteso] Il prompt dichiara un argomento chiamato \"focus\"");

        var prompt = LoggerHelperMcpPrompts.BuildList()
            .Single(p => p.Name == "diagnose-logging");

        _output.WriteLine($"[Restituito] Argomenti=[{string.Join(", ", prompt.Arguments?.Select(a => a.Name) ?? [])}]");
        Assert.NotNull(prompt.Arguments);
        Assert.Contains(prompt.Arguments!, a => a.Name == "focus");
    }

    [Fact]
    public void BuildList_DiagnoseLogging_FocusArgumentIsNotRequired() {
        _output.WriteLine("[Scenario] MCP prompts/list: leggo l'argomento \"focus\" del prompt \"diagnose-logging\"");
        _output.WriteLine("[Atteso] L'argomento non è obbligatorio (Required == false): senza focus si ottiene la diagnosi completa");

        var arg = LoggerHelperMcpPrompts.BuildList()
            .Single(p => p.Name == "diagnose-logging")
            .Arguments!
            .Single(a => a.Name == "focus");

        _output.WriteLine($"[Restituito] Required={arg.Required}");
        Assert.False(arg.Required);
    }

    // ── prompts/get — diagnose-logging (all) ─────────────────────────────────

    [Fact]
    public void Get_DiagnoseAll_ReturnsMcpMessages() {
        _output.WriteLine("[Scenario] MCP prompts/get \"diagnose-logging\" senza focus, serializzato in JSON");
        _output.WriteLine("[Atteso] Il JSON contiene la struttura MCP con \"messages\" e il ruolo \"user\"");

        var result = LoggerHelperMcpPrompts.Get("diagnose-logging");
        var json   = JsonSerializer.Serialize(result);
        _output.WriteLine($"[Restituito] Json.Length={json.Length}, ContieneMessages={json.Contains("messages")}, ContieneUser={json.Contains("user")}");
        Assert.Contains("messages", json);
        Assert.Contains("user", json);
    }

    [Fact]
    public void Get_DiagnoseAll_ContainsAllFourTools() {
        _output.WriteLine("[Scenario] MCP prompts/get \"diagnose-logging\" senza focus");
        _output.WriteLine("[Atteso] Il prompt cita tutti e 4 i tool: get_health, get_errors, get_sinks, get_config");

        var result = JsonSerializer.Serialize(LoggerHelperMcpPrompts.Get("diagnose-logging"));
        _output.WriteLine($"[Restituito] health={result.Contains("loggerhelper_get_health")}, errors={result.Contains("loggerhelper_get_errors")}, sinks={result.Contains("loggerhelper_get_sinks")}, config={result.Contains("loggerhelper_get_config")}");
        Assert.Contains("loggerhelper_get_health",  result);
        Assert.Contains("loggerhelper_get_errors",  result);
        Assert.Contains("loggerhelper_get_sinks",   result);
        Assert.Contains("loggerhelper_get_config",  result);
    }

    [Fact]
    public void Get_DiagnoseAll_ContainsReportHeadings() {
        _output.WriteLine("[Scenario] MCP prompts/get \"diagnose-logging\" senza focus");
        _output.WriteLine("[Atteso] Il prompt chiede un report con le sezioni \"Overall Status\" e \"Recommended Actions\"");

        var result = JsonSerializer.Serialize(LoggerHelperMcpPrompts.Get("diagnose-logging"));
        _output.WriteLine($"[Restituito] OverallStatus={result.Contains("Overall Status")}, RecommendedActions={result.Contains("Recommended Actions")}");
        Assert.Contains("Overall Status",        result);
        Assert.Contains("Recommended Actions",   result);
    }

    // ── prompts/get — focus variants ─────────────────────────────────────────

    [Theory]
    [InlineData("health", "loggerhelper_get_health")]
    [InlineData("errors", "loggerhelper_get_errors")]
    [InlineData("sinks",  "loggerhelper_get_sinks")]
    [InlineData("config", "loggerhelper_get_config")]
    public void Get_DiagnoseWithFocus_MentionsCorrectTool(string focus, string expectedTool) {
        _output.WriteLine($"[Scenario] MCP prompts/get \"diagnose-logging\" con focus=\"{focus}\"");
        _output.WriteLine($"[Atteso] Il prompt cita il tool {expectedTool}");

        var result = JsonSerializer.Serialize(LoggerHelperMcpPrompts.Get("diagnose-logging", focus));
        _output.WriteLine($"[Restituito] Contiene {expectedTool}={result.Contains(expectedTool)}");
        Assert.Contains(expectedTool, result);
    }

    [Fact]
    public void Get_DiagnoseHealth_DoesNotMentionOtherTools() {
        _output.WriteLine("[Scenario] MCP prompts/get \"diagnose-logging\" con focus=\"health\"");
        _output.WriteLine("[Atteso] Il prompt non cita get_errors, get_sinks e get_config (diagnosi mirata, meno token)");

        var result = JsonSerializer.Serialize(LoggerHelperMcpPrompts.Get("diagnose-logging", "health"));
        _output.WriteLine($"[Restituito] errors={result.Contains("loggerhelper_get_errors")}, sinks={result.Contains("loggerhelper_get_sinks")}, config={result.Contains("loggerhelper_get_config")}");
        Assert.DoesNotContain("loggerhelper_get_errors", result);
        Assert.DoesNotContain("loggerhelper_get_sinks",  result);
        Assert.DoesNotContain("loggerhelper_get_config", result);
    }

    // ── prompts/get — error cases ─────────────────────────────────────────────

    [Fact]
    public void Get_UnknownPrompt_ThrowsArgumentException() {
        _output.WriteLine("[Scenario] MCP prompts/get con nome inesistente \"nonexistent-prompt\"");
        _output.WriteLine("[Atteso] ArgumentException");

        var ex = Assert.Throws<ArgumentException>(() => LoggerHelperMcpPrompts.Get("nonexistent-prompt"));
        _output.WriteLine($"[Restituito] Eccezione: {ex.GetType().Name}, Message=\"{ex.Message}\"");
    }

    [Fact]
    public void Get_UnknownPrompt_ExceptionMessageContainsName() {
        _output.WriteLine("[Scenario] MCP prompts/get con nome inesistente \"nonexistent-prompt\", ispeziono il messaggio dell'eccezione");
        _output.WriteLine("[Atteso] Il messaggio contiene il nome richiesto \"nonexistent-prompt\" per facilitare il debug");

        var ex = Assert.Throws<ArgumentException>(
            () => LoggerHelperMcpPrompts.Get("nonexistent-prompt"));
            _output.WriteLine($"[Restituito] Message=\"{ex.Message}\"");
        Assert.Contains("nonexistent-prompt", ex.Message);
    }

    // ── prompt description ────────────────────────────────────────────────────

    [Fact]
    public void Get_DiagnoseAll_ResultHasDescription() {
        _output.WriteLine("[Scenario] MCP prompts/get \"diagnose-logging\" senza focus, serializzato in JSON");
        _output.WriteLine("[Atteso] Il JSON contiene il campo \"description\"");

        var result = JsonSerializer.Serialize(LoggerHelperMcpPrompts.Get("diagnose-logging"));
        _output.WriteLine($"[Restituito] ContieneDescription={result.Contains("description")}");
        Assert.Contains("description", result);
    }
}
