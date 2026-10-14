using Xunit.Abstractions;
using Serilog.Events;

namespace CSharpEssentials.LoggerHelper.Tests;

public class SinkRoutingTests {
    private readonly ITestOutputHelper _output;

    public SinkRoutingTests(ITestOutputHelper output) {
        _output = output;
    }

    [Theory]
    [InlineData("Information", LogEventLevel.Information, true)]
    [InlineData("Error", LogEventLevel.Error, true)]
    [InlineData("Warning", LogEventLevel.Information, false)]
    [InlineData("Error", LogEventLevel.Debug, false)]
    public void Matches_ReturnsCorrectResult(string configuredLevel, LogEventLevel eventLevel, bool expected) {
        _output.WriteLine($"[Scenario] SinkRouting(Console) configurato con il livello \"{configuredLevel}\": verifico Matches({eventLevel})");
        _output.WriteLine($"[Atteso] Matches == {expected}");

        var routing = new SinkRouting {
            Sink = "Console",
            Levels = [configuredLevel]
        };

        _output.WriteLine($"[Restituito] Matches={routing.Matches(eventLevel)}");
        Assert.Equal(expected, routing.Matches(eventLevel));
    }

    [Fact]
    public void Matches_IsCaseInsensitive() {
        _output.WriteLine("[Scenario] SinkRouting con livelli scritti in modo diverso (\"information\", \"ERROR\"): verifico Matches per Information, Error e Warning");
        _output.WriteLine("[Atteso] Il confronto è case-insensitive: Information e Error corrispondono, Warning no");

        var routing = new SinkRouting {
            Sink = "Console",
            Levels = ["information", "ERROR"]
        };

        _output.WriteLine($"[Restituito] Information={routing.Matches(LogEventLevel.Information)}, Error={routing.Matches(LogEventLevel.Error)}, Warning={routing.Matches(LogEventLevel.Warning)}");
        Assert.True(routing.Matches(LogEventLevel.Information));
        Assert.True(routing.Matches(LogEventLevel.Error));
        Assert.False(routing.Matches(LogEventLevel.Warning));
    }

    [Fact]
    public void Matches_EmptyLevels_ReturnsFalse() {
        _output.WriteLine("[Scenario] SinkRouting con lista Levels vuota: verifico Matches(Information)");
        _output.WriteLine("[Atteso] false: senza livelli configurati nessun evento viene instradato al sink");

        var routing = new SinkRouting {
            Sink = "Console",
            Levels = []
        };

        _output.WriteLine($"[Restituito] Matches={routing.Matches(LogEventLevel.Information)}");
        Assert.False(routing.Matches(LogEventLevel.Information));
    }

    [Fact]
    public void Matches_AllLevels_AlwaysReturnsTrue() {
        _output.WriteLine("[Scenario] SinkRouting con tutti i livelli di LogEventLevel (Verbose..Fatal) configurati");
        _output.WriteLine("[Atteso] Matches restituisce true per ogni livello");

        var routing = new SinkRouting {
            Sink = "Console",
            Levels = Enum.GetValues<LogEventLevel>().Select(l => l.ToString()).ToList()
        };

        var matched = Enum.GetValues<LogEventLevel>().Count(routing.Matches);
        var total = Enum.GetValues<LogEventLevel>().Length;
        _output.WriteLine($"[Restituito] Livelli corrispondenti={matched} su {total}");
        Assert.Equal(total, matched);
    }
}
