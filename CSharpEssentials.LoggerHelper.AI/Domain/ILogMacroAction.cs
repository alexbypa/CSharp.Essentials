namespace CSharpEssentials.LoggerHelper.AI.Domain;
public interface ILogMacroAction {
    string Name { get; }                 // es. "SummarizeIncident"
    bool CanExecute(IMacroContext ctx);   // regole veloci
    Task<MacroResult> ExecuteAsync(IMacroContext ctx, CancellationToken ct = default);
    Type ContextType { get; }
}
public interface ILogMacroAction<TContext> : ILogMacroAction where TContext : IMacroContext {
}