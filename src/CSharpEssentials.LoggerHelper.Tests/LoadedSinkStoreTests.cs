using CSharpEssentials.LoggerHelper.Diagnostics;

namespace CSharpEssentials.LoggerHelper.Tests;

public class LoadedSinkStoreTests {
    [Fact]
    public void Add_AndGetAll_ReturnsEntries() {
        var store = new LoadedSinkStore();
        store.Add(new LoadedSinkInfo {
            SinkName = "Console",
            PluginType = "ConsoleSinkPlugin",
            Levels = ["Information"],
            Configured = true
        });

        var all = store.GetAll();
        Assert.Single(all);
        Assert.Equal("Console", all[0].SinkName);
    }
}
