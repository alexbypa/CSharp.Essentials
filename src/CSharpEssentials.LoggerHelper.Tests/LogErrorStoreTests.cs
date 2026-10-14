using Xunit.Abstractions;
using CSharpEssentials.LoggerHelper.Diagnostics;

namespace CSharpEssentials.LoggerHelper.Tests;

public class LogErrorStoreTests {
    private readonly ITestOutputHelper _output;

    public LogErrorStoreTests(ITestOutputHelper output) {
        _output = output;
    }

    [Fact]
    public void Add_StoresEntry() {
        _output.WriteLine("[Scenario] Store vuoto: aggiungo un singolo LogErrorEntry (SinkName=Test, ErrorMessage=fail) tramite Add");
        _output.WriteLine("[Atteso] Count == 1 e GetAll() contiene esattamente un elemento");

        var store = new LogErrorStore();
        store.Add(new LogErrorEntry { SinkName = "Test", ErrorMessage = "fail" });

        _output.WriteLine($"[Restituito] Count={store.Count}, GetAll().Count={store.GetAll().Count}");
        Assert.Equal(1, store.Count);
        Assert.Single(store.GetAll());
    }

    [Fact]
    public void GetAll_ReturnsAllEntries() {
        _output.WriteLine("[Scenario] Aggiungo due errori da sink diversi (A e B) e leggo tutto con GetAll");
        _output.WriteLine("[Atteso] Vengono restituiti 2 elementi: uno per il sink A e uno per il sink B");

        var store = new LogErrorStore();
        store.Add(new LogErrorEntry { SinkName = "A", ErrorMessage = "err1" });
        store.Add(new LogErrorEntry { SinkName = "B", ErrorMessage = "err2" });

        var all = store.GetAll();

        _output.WriteLine($"[Restituito] Count={all.Count}, sinks=[{string.Join(", ", all.Select(e => e.SinkName))}]");
        Assert.Equal(2, all.Count);
        Assert.Contains(all, e => e.SinkName == "A");
        Assert.Contains(all, e => e.SinkName == "B");
    }

    [Fact]
    public void Clear_RemovesAllEntries() {
        _output.WriteLine("[Scenario] Store con 2 errori (sink X e Y), poi chiamo Clear");
        _output.WriteLine("[Atteso] Count == 0 e GetAll() restituisce una collezione vuota");

        var store = new LogErrorStore();
        store.Add(new LogErrorEntry { SinkName = "X", ErrorMessage = "err" });
        store.Add(new LogErrorEntry { SinkName = "Y", ErrorMessage = "err" });

        store.Clear();

        _output.WriteLine($"[Restituito] Count={store.Count}, GetAll().Count={store.GetAll().Count}");
        Assert.Equal(0, store.Count);
        Assert.Empty(store.GetAll());
    }

    [Fact]
    public async Task IsThreadSafe() {
        _output.WriteLine("[Scenario] 100 Task concorrenti chiamano Add sullo stesso store, ognuno con un sink diverso");
        _output.WriteLine("[Atteso] Nessuna eccezione e nessun elemento perso: Count == 100");

        var store = new LogErrorStore();
        var tasks = Enumerable.Range(0, 100).Select(i =>
            Task.Run(() => store.Add(new LogErrorEntry {
                SinkName = $"Sink{i}",
                ErrorMessage = $"Error {i}"
            }))
        ).ToArray();

        await Task.WhenAll(tasks);

        _output.WriteLine($"[Restituito] Count={store.Count}");
        Assert.Equal(100, store.Count);
    }

    [Fact]
    public void LogErrorEntry_DefaultValues() {
        _output.WriteLine("[Scenario] Creo un LogErrorEntry senza valorizzare nessuna proprietà");
        _output.WriteLine("[Atteso] SinkName=\"Unknown\", ErrorMessage vuoto, StackTrace/ContextInfo null, Timestamp <= UtcNow");

        var entry = new LogErrorEntry();

        _output.WriteLine($"[Restituito] SinkName=\"{entry.SinkName}\", ErrorMessage=\"{entry.ErrorMessage}\", StackTrace={entry.StackTrace ?? "null"}, ContextInfo={entry.ContextInfo ?? "null"}, Timestamp={entry.Timestamp:O}");
        Assert.Equal("Unknown", entry.SinkName);
        Assert.Equal(string.Empty, entry.ErrorMessage);
        Assert.Null(entry.StackTrace);
        Assert.Null(entry.ContextInfo);
        Assert.True(entry.Timestamp <= DateTime.UtcNow);
    }
}
