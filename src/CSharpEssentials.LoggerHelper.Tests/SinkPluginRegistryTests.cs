using Serilog;

namespace CSharpEssentials.LoggerHelper.Tests;

public class SinkPluginRegistryTests {
    [Fact]
    public void Register_AddsPluginToRegistry() {
        SinkPluginRegistry.Clear();
        SinkPluginRegistry.Register(new FakeSinkPlugin("TestSink"));

        Assert.Single(SinkPluginRegistry.All);
    }

    [Fact]
    public void FindHandler_ReturnsCorrectPlugin() {
        SinkPluginRegistry.Clear();
        SinkPluginRegistry.Register(new FakeSinkPlugin("UniqueSink_Find"));

        var found = SinkPluginRegistry.FindHandler("UniqueSink_Find");

        Assert.NotNull(found);
        Assert.True(found.CanHandle("UniqueSink_Find"));
    }

    [Fact]
    public void FindHandler_ReturnsNull_WhenNotRegistered() {
        var found = SinkPluginRegistry.FindHandler("NonExistentSink_" + Guid.NewGuid());

        Assert.Null(found);
    }

    [Fact]
    public void ConsoleSink_CanHandle_AfterManualRegistration() {
        // [ModuleInitializer] auto-registers when assembly loads.
        // In test context, we force it by touching the type.
        CSharpEssentials.LoggerHelper.Sink.Console.PluginInitializer.Init();

        var found = SinkPluginRegistry.FindHandler("Console");

        Assert.NotNull(found);
        Assert.True(found!.CanHandle("Console"));
    }

    [Fact]
    public void FileSink_CanHandle_AfterManualRegistration() {
        CSharpEssentials.LoggerHelper.Sink.File.PluginInitializer.Init();

        var found = SinkPluginRegistry.FindHandler("File");

        Assert.NotNull(found);
        Assert.True(found!.CanHandle("File"));
    }

    private sealed class FakeSinkPlugin : ISinkPlugin {
        private readonly string _name;

        internal FakeSinkPlugin(string name) => _name = name;

        public bool CanHandle(string sinkName) => sinkName == _name;
        public void Configure(LoggerConfiguration loggerConfig, SinkRouting routing, LoggerHelperOptions options) { }
    }
}
