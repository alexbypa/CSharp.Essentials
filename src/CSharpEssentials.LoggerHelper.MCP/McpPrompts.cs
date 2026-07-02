namespace CSharpEssentials.LoggerHelper.MCP;

/// <summary>
/// Predefined MCP prompts that guide AI assistants in diagnosing LoggerHelper sinks.
/// Register via <see cref="McpExtensions.AddLoggerHelperMcp"/> — prompts are served
/// automatically through <c>prompts/list</c> and <c>prompts/get</c> on the MCP endpoint.
/// </summary>
public static class LoggerHelperMcpPrompts {
    /// <summary>Returns the list of all predefined prompts (for <c>prompts/list</c>).</summary>
    public static McpPromptDefinition[] BuildList() => [
        new McpPromptDefinition {
            Name        = "diagnose-logging",
            Description = "Run a full diagnostic of LoggerHelper sinks, errors, and configuration. " +
                          "Calls the available tools and produces a structured health report.",
            Arguments   = [
                new McpPromptArgument {
                    Name        = "focus",
                    Description = "Optional focus area: health | errors | sinks | config | all (default: all)",
                    Required    = false
                }
            ]
        }
    ];

    /// <summary>
    /// Returns the prompt messages for the given prompt name.
    /// </summary>
    /// <param name="name">Prompt name (e.g. <c>diagnose-logging</c>).</param>
    /// <param name="focus">Optional focus area: health, errors, sinks, config, or all.</param>
    /// <exception cref="ArgumentException">Thrown when <paramref name="name"/> is not recognised.</exception>
    public static object Get(string name, string focus = "all") =>
        name switch {
            "diagnose-logging" => DiagnosePrompt(focus),
            _ => throw new ArgumentException($"Unknown prompt: '{name}'. Available: diagnose-logging")
        };

    private static object DiagnosePrompt(string focus) {
        var steps = focus switch {
            "health"  => "Call `loggerhelper_get_health` and report the overall status.",
            "errors"  => "Call `loggerhelper_get_errors` with count=50 and analyse every error entry.",
            "sinks"   => "Call `loggerhelper_get_sinks` and list all FAILED sinks with their most recent error.",
            "config"  => "Call `loggerhelper_get_config` and highlight any routing or masking misconfiguration.",
            _         => """
                         Call all four tools in this order:
                         1. `loggerhelper_get_health`           — overall status (OK / WARNING / CRITICAL)
                         2. `loggerhelper_get_errors` (count=50) — recent sink errors with context
                         3. `loggerhelper_get_sinks`             — per-sink ACTIVE / FAILED status
                         4. `loggerhelper_get_config`            — routing rules and masking settings
                         """
        };

        var text =
            $"You are a LoggerHelper diagnostic assistant. {steps}\n\n" +
            "After gathering the data, produce a concise structured report using these headings:\n\n" +
            "## Overall Status\n" +
            "## Active Sinks  (name + log levels)\n" +
            "## Failed Sinks  (name + most recent error)\n" +
            "## Configuration Issues  (misrouted levels, missing sinks, disabled masking)\n" +
            "## Recommended Actions  (concrete, numbered steps to fix each issue)\n\n" +
            "Be specific and actionable. Skip headings that have nothing to report.";

        return new {
            description = "Diagnose LoggerHelper sink health and configuration",
            messages    = new[] {
                new { role = "user", content = new { type = "text", text } }
            }
        };
    }
}
