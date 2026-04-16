using CSharpEssentials.LoggerHelper.Diagnostics;

namespace CSharpEssentials.LoggerHelper.Tests;

public class SinkThrottlingManagerTests {
    [Fact]
    public void CanSend_FirstCall_ReturnsTrue() {
        var sinkName = $"Throttle_First_{Guid.NewGuid()}";
        Assert.True(SinkThrottlingManager.CanSend(sinkName, TimeSpan.FromMinutes(1)));
    }

    [Fact]
    public void CanSend_WithinInterval_ReturnsFalse() {
        var sinkName = $"Throttle_Block_{Guid.NewGuid()}";

        Assert.True(SinkThrottlingManager.CanSend(sinkName, TimeSpan.FromMinutes(5)));
        Assert.False(SinkThrottlingManager.CanSend(sinkName, TimeSpan.FromMinutes(5)));
    }

    [Fact]
    public void CanSend_ZeroInterval_AlwaysReturnsTrue() {
        var sinkName = $"Throttle_Zero_{Guid.NewGuid()}";

        Assert.True(SinkThrottlingManager.CanSend(sinkName, TimeSpan.Zero));
        Assert.True(SinkThrottlingManager.CanSend(sinkName, TimeSpan.Zero));
        Assert.True(SinkThrottlingManager.CanSend(sinkName, TimeSpan.Zero));
    }

    [Fact]
    public void CanSend_DifferentSinks_AreIndependent() {
        var sink1 = $"Throttle_A_{Guid.NewGuid()}";
        var sink2 = $"Throttle_B_{Guid.NewGuid()}";

        Assert.True(SinkThrottlingManager.CanSend(sink1, TimeSpan.FromMinutes(5)));
        Assert.True(SinkThrottlingManager.CanSend(sink2, TimeSpan.FromMinutes(5)));

        // sink1 is throttled, but sink2 should also be throttled independently
        Assert.False(SinkThrottlingManager.CanSend(sink1, TimeSpan.FromMinutes(5)));
        Assert.False(SinkThrottlingManager.CanSend(sink2, TimeSpan.FromMinutes(5)));
    }
}
