using CSharpEssentials.LoggerHelper.AI.Domain;

namespace CSharpEssentials.LoggerHelper.AI.Application;
public interface IActionOrchestrator {
    Task<IReadOnlyList<MacroResult>> RunAsync(IMacroContext ctx, CancellationToken ct = default);
}
public sealed class ActionOrchestrator : IActionOrchestrator {
    private readonly IEnumerable<ILogMacroAction> _actions;
    public ActionOrchestrator(IEnumerable<ILogMacroAction> actions) => _actions = actions;
    public async Task<IReadOnlyList<MacroResult>> RunAsync(IMacroContext ctx, CancellationToken ct = default) {
        var results = new List<MacroResult>();
        foreach (var a in _actions.Distinct())
            if (a.CanExecute(ctx))
                if (a.Name.Equals(ctx.action, StringComparison.InvariantCultureIgnoreCase)) {
                    try {
                        results.Add(await a.ExecuteAsync(ctx, ct));
                    }catch (Exception ex) {
                        results.Add(new MacroResult(a.Name, "Exception", new Dictionary<string, object> { {  "Exception", ex.Message } }));
                    }
                }
        return results;
    }
}