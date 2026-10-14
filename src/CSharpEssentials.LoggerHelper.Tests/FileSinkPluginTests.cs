using CSharpEssentials.LoggerHelper.Sink.File;
using Serilog;

namespace CSharpEssentials.LoggerHelper.Tests;

public class FileSinkPluginTests {
    private static void Configure(string interval, string? fileNameProperty = null) {
        var options = new LoggerHelperOptions();
        options.SetSinkConfig("File", new FileSinkOptions {
            Path = Path.Combine(Path.GetTempPath(), "lh-file-sink-tests", "log-.txt"),
            RollingInterval = interval,
            FileNameProperty = fileNameProperty
        });
        new FileSinkPlugin().Configure(new LoggerConfiguration(), new SinkRouting { Sink = "File", Levels = ["Error"] }, options);
    }

    [Theory]
    [InlineData("day")]
    [InlineData("HOUR")]
    [InlineData("Infinite")]
    public void Configure_RollingIntervalAnyCase_DoesNotThrow(string interval) {
        Configure(interval);
        Configure(interval, "TenantId");
    }

    [Theory]
    [InlineData("Daily")]
    [InlineData("999")]
    [InlineData("")]
    public void Configure_InvalidRollingInterval_ThrowsInvalidOperationException(string interval) {
        var ex = Assert.Throws<InvalidOperationException>(() => Configure(interval));
        Assert.Contains("RollingInterval", ex.Message);
        Assert.Throws<InvalidOperationException>(() => Configure(interval, "TenantId"));
    }
}
