using CSharpEssentials.LoggerHelper.model;
using System.Collections.Concurrent;

namespace CSharpEssentials.LoggerHelper;
public class LoggerErrorStore {
    private readonly ConcurrentQueue<LogErrorEntry> _errors = new();

    public void Add(LogErrorEntry entry) => _errors.Enqueue(entry);

    public IEnumerable<LogErrorEntry> GetAll() => _errors.ToList();

    public void Clear() => _errors.Clear();
}
