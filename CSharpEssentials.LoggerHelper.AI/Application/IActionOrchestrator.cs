using CSharpEssentials.LoggerHelper.AI.Domain;

namespace CSharpEssentials.LoggerHelper.AI.Application;
public interface IActionOrchestrator {
    Task<IReadOnlyList<MacroResult>> RunAsync(MacroContext ctx, CancellationToken ct = default);
}

public sealed class ActionOrchestrator : IActionOrchestrator {
    private readonly IEnumerable<ILogMacroAction> _actions;
    public ActionOrchestrator(IEnumerable<ILogMacroAction> actions) => _actions = actions;
    public async Task<IReadOnlyList<MacroResult>> RunAsync(MacroContext ctx, CancellationToken ct = default) {
        var results = new List<MacroResult>();
        foreach (var a in _actions)
            if (a.CanExecute(ctx))
                if (a is RagAnswerQueryAction) {
                    results.Add(await a.ExecuteAsync(ctx, ct));
                }
        return results;
    }
}
