using CSharpEssentials.LoggerHelper.Sink.Console;
using CSharpEssentials.LoggerHelper.Sink.File;
using Serilog.Events;

namespace CSharpEssentials.LoggerHelper.Tests;

public class LoggerHelperBuilderTests {
    [Fact]
    public void WithApplicationName_SetsName() {
        var builder = new LoggerHelperBuilder();
        builder.WithApplicationName("MyApp");

        Assert.Equal("MyApp", builder.Options.ApplicationName);
    }

    [Fact]
    public void AddRoute_AddsRoutingRule() {
        var builder = new LoggerHelperBuilder();
        builder.AddRoute("Console", LogEventLevel.Information, LogEventLevel.Error);

        Assert.Single(builder.Options.Routes);
        Assert.Equal("Console", builder.Options.Routes[0].Sink);
        Assert.Equal(2, builder.Options.Routes[0].Levels.Count);
        Assert.Contains("Information", builder.Options.Routes[0].Levels);
        Assert.Contains("Error", builder.Options.Routes[0].Levels);
    }

    [Fact]
    public void AddRouteAll_AddsAllLevels() {
        var builder = new LoggerHelperBuilder();
        builder.AddRouteAll("File");

        Assert.Single(builder.Options.Routes);
        var levels = builder.Options.Routes[0].Levels;
        Assert.Equal(Enum.GetValues<LogEventLevel>().Length, levels.Count);
    }

    [Fact]
    public void MultipleRoutes_AreAdditive() {
        var builder = new LoggerHelperBuilder();
        builder
            .AddRoute("Console", LogEventLevel.Information)
            .AddRoute("Email", LogEventLevel.Error)
            .AddRoute("File", LogEventLevel.Warning, LogEventLevel.Error);

        Assert.Equal(3, builder.Options.Routes.Count);
    }

    [Fact]
    public void ConfigureSink_Generic_SetsOptions() {
        var builder = new LoggerHelperBuilder();
        builder.ConfigureSink<TestSinkOptions>("TestSink", o => o.Value = "hello");

        var opts = builder.Options.GetSinkConfig<TestSinkOptions>("TestSink");
        Assert.NotNull(opts);
        Assert.Equal("hello", opts!.Value);
    }

    [Fact]
    public void ConfigureSink_CalledTwice_MergesOptions() {
        var builder = new LoggerHelperBuilder();
        builder.ConfigureSink<TestSinkOptions>("TestSink", o => o.Value = "first");
        builder.ConfigureSink<TestSinkOptions>("TestSink", o => o.Extra = 42);

        var opts = builder.Options.GetSinkConfig<TestSinkOptions>("TestSink");
        Assert.NotNull(opts);
        Assert.Equal("first", opts!.Value);
        Assert.Equal(42, opts.Extra);
    }

    [Fact]
    public void ConfigureConsole_ExtensionMethod_SetsOptions() {
        var builder = new LoggerHelperBuilder();
        builder.ConfigureConsole(c => c.OutputTemplate = "[{Level}] {Message}");

        var opts = builder.Options.GetSinkConfig<ConsoleSinkOptions>("Console");
        Assert.NotNull(opts);
        Assert.Equal("[{Level}] {Message}", opts!.OutputTemplate);
    }

    [Fact]
    public void ConfigureFile_ExtensionMethod_SetsOptions() {
        var builder = new LoggerHelperBuilder();
        builder.ConfigureFile(f => {
            f.Path = "/var/logs";
            f.RetainedFileCountLimit = 30;
        });

        var opts = builder.Options.GetSinkConfig<FileSinkOptions>("File");
        Assert.NotNull(opts);
        Assert.Equal("/var/logs", opts!.Path);
        Assert.Equal(30, opts.RetainedFileCountLimit);
    }

    [Fact]
    public void FluentChaining_Works() {
        var builder = new LoggerHelperBuilder();

        var result = builder
            .WithApplicationName("ChainTest")
            .AddRoute("Console", LogEventLevel.Information)
            .AddRoute("Email", LogEventLevel.Error)
            .ConfigureSink<TestSinkOptions>("Email", e => e.Value = "test@test.com")
            .EnableSelfLogging()
            .EnableRequestResponseLogging()
            .DisableOpenTelemetry();

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
