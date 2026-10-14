using Xunit.Abstractions;
using CSharpEssentials.LoggerHelper.Sink.Console;
using CSharpEssentials.LoggerHelper.Sink.File;
using Serilog.Events;

namespace CSharpEssentials.LoggerHelper.Tests;

public class LoggerHelperBuilderTests {
    private readonly ITestOutputHelper _output;

    public LoggerHelperBuilderTests(ITestOutputHelper output) {
        _output = output;
    }

    [Fact]
    public void WithApplicationName_SetsName() {
        _output.WriteLine("[Scenario] LoggerHelperBuilder: chiamo WithApplicationName(\"MyApp\")");
        _output.WriteLine("[Atteso] Options.ApplicationName == \"MyApp\"");

        var builder = new LoggerHelperBuilder();
        builder.WithApplicationName("MyApp");

        _output.WriteLine($"[Restituito] ApplicationName={builder.Options.ApplicationName}");
        Assert.Equal("MyApp", builder.Options.ApplicationName);
    }

    [Fact]
    public void AddRoute_AddsRoutingRule() {
        _output.WriteLine("[Scenario] AddRoute(\"Console\", Information, Error)");
        _output.WriteLine("[Atteso] Una sola route per il sink Console con 2 livelli (Information ed Error)");

        var builder = new LoggerHelperBuilder();
        builder.AddRoute("Console", LogEventLevel.Information, LogEventLevel.Error);

        _output.WriteLine($"[Restituito] Routes={builder.Options.Routes.Count}, Sink={builder.Options.Routes[0].Sink}, Levels=[{string.Join(", ", builder.Options.Routes[0].Levels)}]");
        Assert.Single(builder.Options.Routes);
        Assert.Equal("Console", builder.Options.Routes[0].Sink);
        Assert.Equal(2, builder.Options.Routes[0].Levels.Count);
        Assert.Contains("Information", builder.Options.Routes[0].Levels);
        Assert.Contains("Error", builder.Options.Routes[0].Levels);
    }

    [Fact]
    public void AddRouteAll_AddsAllLevels() {
        _output.WriteLine("[Scenario] AddRouteAll(\"File\")");
        _output.WriteLine("[Atteso] Una sola route per File con tutti i livelli di LogEventLevel");

        var builder = new LoggerHelperBuilder();
        builder.AddRouteAll("File");

        _output.WriteLine($"[Restituito] Routes={builder.Options.Routes.Count}, Levels={builder.Options.Routes[0].Levels.Count} (attesi {Enum.GetValues<LogEventLevel>().Length})");
        Assert.Single(builder.Options.Routes);
        var levels = builder.Options.Routes[0].Levels;
        Assert.Equal(Enum.GetValues<LogEventLevel>().Length, levels.Count);
    }

    [Fact]
    public void MultipleRoutes_AreAdditive() {
        _output.WriteLine("[Scenario] Tre AddRoute concatenate (Console, Email, File)");
        _output.WriteLine("[Atteso] Le route si sommano: Routes.Count == 3");

        var builder = new LoggerHelperBuilder();
        builder
            .AddRoute("Console", LogEventLevel.Information)
            .AddRoute("Email", LogEventLevel.Error)
            .AddRoute("File", LogEventLevel.Warning, LogEventLevel.Error);

        _output.WriteLine($"[Restituito] Routes={builder.Options.Routes.Count}");
        Assert.Equal(3, builder.Options.Routes.Count);
    }

    [Fact]
    public void ConfigureSink_Generic_SetsOptions() {
        _output.WriteLine("[Scenario] ConfigureSink<TestSinkOptions>(\"TestSink\", o => o.Value = \"hello\") e lettura con GetSinkConfig");
        _output.WriteLine("[Atteso] Opzioni presenti con Value == \"hello\"");

        var builder = new LoggerHelperBuilder();
        builder.ConfigureSink<TestSinkOptions>("TestSink", o => o.Value = "hello");

        var opts = builder.Options.GetSinkConfig<TestSinkOptions>("TestSink");
        _output.WriteLine($"[Restituito] Value={opts?.Value}");
        Assert.NotNull(opts);
        Assert.Equal("hello", opts!.Value);
    }

    [Fact]
    public void ConfigureSink_CalledTwice_MergesOptions() {
        _output.WriteLine("[Scenario] ConfigureSink sullo stesso sink \"TestSink\" due volte: prima Value=\"first\", poi Extra=42");
        _output.WriteLine("[Atteso] Le configurazioni si fondono sulla stessa istanza: Value==\"first\" ed Extra==42");

        var builder = new LoggerHelperBuilder();
        builder.ConfigureSink<TestSinkOptions>("TestSink", o => o.Value = "first");
        builder.ConfigureSink<TestSinkOptions>("TestSink", o => o.Extra = 42);

        var opts = builder.Options.GetSinkConfig<TestSinkOptions>("TestSink");
        _output.WriteLine($"[Restituito] Value={opts?.Value}, Extra={opts?.Extra}");
        Assert.NotNull(opts);
        Assert.Equal("first", opts!.Value);
        Assert.Equal(42, opts.Extra);
    }

    [Fact]
    public void ConfigureConsole_ExtensionMethod_SetsOptions() {
        _output.WriteLine("[Scenario] ConfigureConsole(c => c.OutputTemplate = \"[{Level}] {Message}\")");
        _output.WriteLine("[Atteso] GetSinkConfig<ConsoleSinkOptions>(\"Console\") restituisce OutputTemplate \"[{Level}] {Message}\"");

        var builder = new LoggerHelperBuilder();
        builder.ConfigureConsole(c => c.OutputTemplate = "[{Level}] {Message}");

        var opts = builder.Options.GetSinkConfig<ConsoleSinkOptions>("Console");
        _output.WriteLine($"[Restituito] OutputTemplate={opts?.OutputTemplate}");
        Assert.NotNull(opts);
        Assert.Equal("[{Level}] {Message}", opts!.OutputTemplate);
    }

    [Fact]
    public void ConfigureFile_ExtensionMethod_SetsOptions() {
        _output.WriteLine("[Scenario] ConfigureFile con Path=\"/var/logs\" e RetainedFileCountLimit=30");
        _output.WriteLine("[Atteso] GetSinkConfig<FileSinkOptions>(\"File\") restituisce Path \"/var/logs\" e RetainedFileCountLimit 30");

        var builder = new LoggerHelperBuilder();
        builder.ConfigureFile(f => {
            f.Path = "/var/logs";
            f.RetainedFileCountLimit = 30;
        });

        var opts = builder.Options.GetSinkConfig<FileSinkOptions>("File");
        _output.WriteLine($"[Restituito] Path={opts?.Path}, RetainedFileCountLimit={opts?.RetainedFileCountLimit}");
        Assert.NotNull(opts);
        Assert.Equal("/var/logs", opts!.Path);
        Assert.Equal(30, opts.RetainedFileCountLimit);
    }

    [Fact]
    public void FluentChaining_Works() {
        _output.WriteLine("[Scenario] Catena fluent: WithApplicationName, 2 AddRoute, ConfigureSink, EnableSelfLogging, EnableRequestResponseLogging, DisableOpenTelemetry");
        _output.WriteLine("[Atteso] Ogni metodo restituisce lo stesso builder e tutte le impostazioni risultano applicate (nome, 2 route, SelfLogging=true, RequestResponseLogging=true, OpenTelemetry=false)");

        var builder = new LoggerHelperBuilder();

        var result = builder
            .WithApplicationName("ChainTest")
            .AddRoute("Console", LogEventLevel.Information)
            .AddRoute("Email", LogEventLevel.Error)
            .ConfigureSink<TestSinkOptions>("Email", e => e.Value = "test@test.com")
            .EnableSelfLogging()
            .EnableRequestResponseLogging()
            .DisableOpenTelemetry();

        _output.WriteLine($"[Restituito] SameBuilder={ReferenceEquals(builder, result)}, Name={builder.Options.ApplicationName}, Routes={builder.Options.Routes.Count}, SelfLogging={builder.Options.General.EnableSelfLogging}, ReqResp={builder.Options.General.EnableRequestResponseLogging}, OTel={builder.Options.General.EnableOpenTelemetry}");
        Assert.Same(builder, result);
        Assert.Equal("ChainTest", builder.Options.ApplicationName);
        Assert.Equal(2, builder.Options.Routes.Count);
        Assert.True(builder.Options.General.EnableSelfLogging);
        Assert.True(builder.Options.General.EnableRequestResponseLogging);
        Assert.False(builder.Options.General.EnableOpenTelemetry);
    }

    // Simple options class for testing the generic ConfigureSink<T> mechanism
    internal sealed class TestSinkOptions {
        public string Value { get; set; } = string.Empty;
        public int Extra { get; set; }
    }
}
