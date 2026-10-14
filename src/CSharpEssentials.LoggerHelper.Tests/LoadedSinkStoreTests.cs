using Xunit.Abstractions;
using CSharpEssentials.LoggerHelper.Diagnostics;

namespace CSharpEssentials.LoggerHelper.Tests;

public class LoadedSinkStoreTests {
    private readonly ITestOutputHelper _output;

    public LoadedSinkStoreTests(ITestOutputHelper output) {
        _output = output;
    }

    [Fact]
    public void Add_AndGetAll_ReturnsEntries() {
        _output.WriteLine("[Scenario] Aggiungo allo store un sink caricato (Console, plugin ConsoleSinkPlugin, livello Information, Configured=true) e leggo con GetAll");
        _output.WriteLine("[Atteso] GetAll restituisce un solo elemento con SinkName == \"Console\"");

        var store = new LoadedSinkStore();
        store.Add(new LoadedSinkInfo {
            SinkName = "Console",
            PluginType = "ConsoleSinkPlugin",
            Levels = ["Information"],
            Configured = true
        });

        var all = store.GetAll();
        _output.WriteLine($"[Restituito] Count={all.Count}, SinkName={all.FirstOrDefault()?.SinkName}");
        Assert.Single(all);
        Assert.Equal("Console", all[0].SinkName);
    }
}
