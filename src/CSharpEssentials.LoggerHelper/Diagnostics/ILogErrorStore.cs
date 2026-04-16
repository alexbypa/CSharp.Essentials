namespace CSharpEssentials.LoggerHelper.Diagnostics;

/// <summary>
/// Abstraction for the error store.
/// Enables dependency injection and testability.
/// </summary>
public interface ILogErrorStore {
    void Add(LogErrorEntry entry);
    IReadOnlyList<LogErrorEntry> GetAll();
    void Clear();
    int Count { get; }
}
