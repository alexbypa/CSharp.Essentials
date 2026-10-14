using CSharpEssentials.LoggerHelper.Diagnostics;
using Xunit.Abstractions;

namespace CSharpEssentials.LoggerHelper.Tests;

public class SinkThrottlingManagerTests {
    private readonly ITestOutputHelper _output;

    public SinkThrottlingManagerTests(ITestOutputHelper output) {
        _output = output;
    }

    [Fact]
    public void CanSend_FirstCall_ReturnsTrue() {
        var sinkName = $"Throttle_First_{Guid.NewGuid()}";
        var interval = TimeSpan.FromMinutes(1);

        _output.WriteLine($"🚀 [Scenario] Primo invio per il sink: {sinkName}");
        _output.WriteLine($"⏱️ [Parametro Intervallo]: {interval}");

        var result = SinkThrottlingManager.CanSend(sinkName, interval);

        _output.WriteLine($"🎯 [Atteso]: True");
        _output.WriteLine($"📦 [Restituito]: {result}");

        Assert.True(result);
    }

    [Fact]
    public void CanSend_WithinInterval_ReturnsFalse() {
        var sinkName = $"Throttle_Block_{Guid.NewGuid()}";
        var interval = TimeSpan.FromMinutes(5);

        _output.WriteLine($"🚀 [Scenario] Test blocco throttling entro l'intervallo per: {sinkName}");

        // Primo invito
        var firstCall = SinkThrottlingManager.CanSend(sinkName, interval);
        _output.WriteLine($"1️⃣ [Primo Invio] Atteso: True | Restituito: {firstCall}");
        Assert.True(firstCall);

        // Secondo invio immediato (entro l'intervallo)
        var secondCall = SinkThrottlingManager.CanSend(sinkName, interval);
        _output.WriteLine($"2️⃣ [Secondo Invio Immediato] Atteso: False | Restituito: {secondCall}");

        Assert.False(secondCall);
    }

    [Fact]
    public void CanSend_ZeroInterval_AlwaysReturnsTrue() {
        var sinkName = $"Throttle_Zero_{Guid.NewGuid()}";
        var interval = TimeSpan.Zero;

        _output.WriteLine($"🚀 [Scenario] Test intervallo zero (nessun throttling) per: {sinkName}");

        for (int i = 1; i <= 3; i++) {
            var result = SinkThrottlingManager.CanSend(sinkName, interval);
            _output.WriteLine($"🔄 [Tentativo {i}] Atteso: True | Restituito: {result}");
            Assert.True(result);
        }
    }

    [Fact]
    public void CanSend_DifferentSinks_AreIndependent() {
        var sink1 = $"Throttle_A_{Guid.NewGuid()}";
        var sink2 = $"Throttle_B_{Guid.NewGuid()}";
        var interval = TimeSpan.FromMinutes(5);

        _output.WriteLine($"🚀 [Scenario] Test indipendenza tra sink diversi: [{sink1}] e [{sink2}]");

        var call1 = SinkThrottlingManager.CanSend(sink1, interval);
        _output.WriteLine($"👉 [Invio Sink 1] Atteso: True | Restituito: {call1}");
        Assert.True(call1);

        var call2 = SinkThrottlingManager.CanSend(sink2, interval);
        _output.WriteLine($"👉 [Invio Sink 2] Atteso: True | Restituito: {call2}");
        Assert.True(call2);

        // Verifica blocco indipendente
        var block1 = SinkThrottlingManager.CanSend(sink1, interval);
        _output.WriteLine($"🔒 [Secondo Invio Sink 1] Atteso: False | Restituito: {block1}");
        Assert.False(block1);

        var block2 = SinkThrottlingManager.CanSend(sink2, interval);
        _output.WriteLine($"🔒 [Secondo Invio Sink 2] Atteso: False | Restituito: {block2}");
        Assert.False(block2);
    }
}