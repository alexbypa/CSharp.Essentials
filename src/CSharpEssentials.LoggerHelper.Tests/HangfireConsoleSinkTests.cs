using Xunit.Abstractions;
using CSharpEssentials.LoggerHelper.Sink.HangfireConsole;
using Serilog.Events;
using Serilog.Parsing;

namespace CSharpEssentials.LoggerHelper.Tests;

public class HangfireConsoleSinkTests {
    private readonly ITestOutputHelper _output;

    public HangfireConsoleSinkTests(ITestOutputHelper output) {
        _output = output;
    }

    private static LogEvent Event(LogEventLevel level, Exception? ex) =>
        new(DateTimeOffset.UtcNow, level, ex, new MessageTemplateParser().Parse("msg"), []);

    private static Exception Thrown() {
        try {
            throw new HttpRequestException("boom", new TimeoutException("inner"));
        } catch (Exception ex) {
            return ex;
        }
    }

    [Fact]
    public void FormatException_NoException_ReturnsNull() {
        _output.WriteLine("[Scenario] LogWarning(\"msg\") senza eccezione allegata");
        _output.WriteLine("[Atteso] nessuna riga aggiuntiva (null)");

        var text = HangfireConsoleSerilogSink.FormatException(Event(LogEventLevel.Warning, null));

        _output.WriteLine($"[Restituito] {text ?? "null"}");
        Assert.Null(text);
    }

    [Theory]
    [InlineData(LogEventLevel.Debug)]
    [InlineData(LogEventLevel.Information)]
    [InlineData(LogEventLevel.Warning)]
    public void FormatException_BelowError_IsCompactTypeAndMessage(LogEventLevel level) {
        _output.WriteLine($"[Scenario] Log{level}(new HttpRequestException(\"boom\"), \"msg\")");
        _output.WriteLine("[Atteso] \"HttpRequestException: boom\" senza stack trace");

        var text = HangfireConsoleSerilogSink.FormatException(Event(level, Thrown()));

        _output.WriteLine($"[Restituito] {text}");
        Assert.Equal("HttpRequestException: boom", text);
    }

    [Theory]
    [InlineData(LogEventLevel.Error)]
    [InlineData(LogEventLevel.Fatal)]
    public void FormatException_ErrorOrAbove_IsFullToString(LogEventLevel level) {
        _output.WriteLine($"[Scenario] Log{level}(new HttpRequestException(\"boom\", inner), \"msg\")");
        _output.WriteLine("[Atteso] ex.ToString(): tipo completo, messaggio, inner exception e stack trace");

        var ex = Thrown();
        var text = HangfireConsoleSerilogSink.FormatException(Event(level, ex));

        _output.WriteLine($"[Restituito] {text}");
        Assert.Equal(ex.ToString(), text);
        Assert.Contains("System.Net.Http.HttpRequestException: boom", text);
        Assert.Contains("TimeoutException: inner", text);
        Assert.Contains(nameof(Thrown), text);
    }
}
