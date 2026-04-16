using CSharpEssentials.LoggerHelper.Diagnostics;

namespace CSharpEssentials.LoggerHelper.Tests;

public class LogErrorStoreTests {
    [Fact]
    public void Add_StoresEntry() {
        var store = new LogErrorStore();
        store.Add(new LogErrorEntry { SinkName = "Test", ErrorMessage = "fail" });

        Assert.Equal(1, store.Count);
        Assert.Single(store.GetAll());
    }

    [Fact]
    public void GetAll_ReturnsAllEntries() {
        var store = new LogErrorStore();
        store.Add(new LogErrorEntry { SinkName = "A", ErrorMessage = "err1" });
        store.Add(new LogErrorEntry { SinkName = "B", ErrorMessage = "err2" });

        var all = store.GetAll();

        Assert.Equal(2, all.Count);
        Assert.Contains(all, e => e.SinkName == "A");
        Assert.Contains(all, e => e.SinkName == "B");
    }

    [Fact]
    public void Clear_RemovesAllEntries() {
        var store = new LogErrorStore();
        store.Add(new LogErrorEntry { SinkName = "X", ErrorMessage = "err" });
        store.Add(new LogErrorEntry { SinkName = "Y", ErrorMessage = "err" });

        store.Clear();

        Assert.Equal(0, store.Count);
        Assert.Empty(store.GetAll());
    }

    [Fact]
    public async Task IsThreadSafe() {
        var store = new LogErrorStore();
        var tasks = Enumerable.Range(0, 100).Select(i =>
            Task.Run(() => store.Add(new LogErrorEntry {
                SinkName = $"Sink{i}",
                ErrorMessage = $"Error {i}"
            }))
        ).ToArray();

        await Task.WhenAll(tasks);

        Assert.Equal(100, store.Count);
    }

    [Fact]
    public void LogErrorEntry_DefaultValues() {
        var entry = new LogErrorEntry();

        Assert.Equal("Unknown", entry.SinkName);
        Assert.Equal(string.Empty, entry.ErrorMessage);
        Assert.Null(entry.StackTrace);
        Assert.Null(entry.ContextInfo);
        Assert.True(entry.Timestamp <= DateTime.UtcNow);
    }
}
