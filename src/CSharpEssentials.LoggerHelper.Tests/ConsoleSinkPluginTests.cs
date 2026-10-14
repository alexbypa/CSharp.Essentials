using CSharpEssentials.LoggerHelper.Sink.Console;
using Serilog;

namespace CSharpEssentials.LoggerHelper.Tests;

public class ConsoleSinkPluginTests {
    private static string LogHello(string? template, Exception? ex = null) {
        var options = new LoggerHelperOptions();
        options.SetSinkConfig("Console", new ConsoleSinkOptions { OutputTemplate = template });
        var config = new LoggerConfiguration().MinimumLevel.Verbose();
        new ConsoleSinkPlugin().Configure(config, new SinkRouting { Sink = "Console", Levels = ["Information", "Error"] }, options);

        var original = Console.Out;
        var sw = new StringWriter();
        Console.SetOut(sw);
        try {
            using var logger = config.CreateLogger();
            if (ex is null) logger.Information("hello");
            else logger.Error(ex, "hello");
        } finally {
            Console.SetOut(original);
        }
        return sw.ToString();
    }

    [Fact]
    public void Configure_WithOutputTemplate_AppliesTemplate() {
        var output = LogHello("{Level:u3}|{Message}");
        Assert.Contains("INF|hello", output);
        Assert.DoesNotContain("Information]", output);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("  ")]
    public void Configure_NoTemplate_UsesDefaultFormat(string? template) {
        Assert.Contains("Information] hello", LogHello(template));
    }

    [Fact]
    public void Configure_TemplateWithException_RendersException() {
        var output = LogHello("{Message}|{Exception}", new InvalidOperationException("boom"));
        Assert.Contains("hello|", output);
        Assert.Contains("boom", output);
    }

    [Fact]
    public void Configure_NoTemplateWithException_AppendsException() {
        Assert.Contains("boom", LogHello(null, new InvalidOperationException("boom")));
    }
}
