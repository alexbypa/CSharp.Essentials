using Serilog.Events;

namespace CSharpEssentials.LoggerHelper.Tests;

public class SinkRoutingTests {
    [Theory]
    [InlineData("Information", LogEventLevel.Information, true)]
    [InlineData("Error", LogEventLevel.Error, true)]
    [InlineData("Warning", LogEventLevel.Information, false)]
    [InlineData("Error", LogEventLevel.Debug, false)]
    public void Matches_ReturnsCorrectResult(string configuredLevel, LogEventLevel eventLevel, bool expected) {
        var routing = new SinkRouting {
            Sink = "Console",
            Levels = [configuredLevel]
        };

        Assert.Equal(expected, routing.Matches(eventLevel));
    }

    [Fact]
    public void Matches_IsCaseInsensitive() {
        var routing = new SinkRouting {
            Sink = "Console",
            Levels = ["information", "ERROR"]
        };

        Assert.True(routing.Matches(LogEventLevel.Information));
        Assert.True(routing.Matches(LogEventLevel.Error));
        Assert.False(routing.Matches(LogEventLevel.Warning));
    }

    [Fact]
    public void Matches_EmptyLevels_ReturnsFalse() {
        var routing = new SinkRouting {
            Sink = "Console",
            Levels = []
        };

        Assert.False(routing.Matches(LogEventLevel.Information));
    }

    [Fact]
    public void Matches_AllLevels_AlwaysReturnsTrue() {
        var routing = new SinkRouting {
            Sink = "Console",
            Levels = Enum.GetValues<LogEventLevel>().Select(l => l.ToString()).ToList()
        };

        foreach (var level in Enum.GetValues<LogEventLevel>())
            Assert.True(routing.Matches(level));
    }
}
