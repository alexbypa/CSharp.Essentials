using CSharpEssentials.LoggerHelper.Diagnostics;
using Serilog.Debugging;
using Serilog.Events;
using Serilog.Parsing;

namespace CSharpEssentials.LoggerHelper.Tests;

public class ThrottleGateTests {
    private sealed class FakeTimeProvider : TimeProvider {
        private long _ticks;
        public override long TimestampFrequency => TimeSpan.TicksPerSecond;
        public override long GetTimestamp() => Interlocked.Read(ref _ticks);
        public void Advance(TimeSpan by) => Interlocked.Add(ref _ticks, by.Ticks);
    }

    private static LogEvent Evt(string message = "hello") =>
        new(DateTimeOffset.UtcNow, LogEventLevel.Information, null, new MessageTemplateParser().Parse(message), []);

    private static readonly TimeSpan Interval = TimeSpan.FromSeconds(10);

    [Fact]
    public void TryPass_NullInterval_AlwaysTrue() {
        var gate = new ThrottleGate("S", null, new FakeTimeProvider());
        Assert.True(gate.TryPass(Evt()));
        Assert.True(gate.TryPass(Evt()));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-5)]
    public void TryPass_ZeroOrNegativeInterval_AlwaysTrue(int seconds) {
        var gate = new ThrottleGate("S", TimeSpan.FromSeconds(seconds), new FakeTimeProvider());
        Assert.True(gate.TryPass(Evt()));
        Assert.True(gate.TryPass(Evt()));
    }

    [Fact]
    public void TryPass_FirstCall_True() {
        var gate = new ThrottleGate("S", Interval, new FakeTimeProvider());
        Assert.True(gate.TryPass(Evt()));
    }

    [Fact]
    public void TryPass_WithinInterval_False() {
        var clock = new FakeTimeProvider();
        var gate = new ThrottleGate("S", Interval, clock);
        gate.TryPass(Evt());
        clock.Advance(Interval - TimeSpan.FromTicks(1));
        Assert.False(gate.TryPass(Evt()));
    }

    [Fact]
    public void TryPass_AfterAdvancePastInterval_True() {
        var clock = new FakeTimeProvider();
        var gate = new ThrottleGate("S", Interval, clock);
        gate.TryPass(Evt());
        clock.Advance(Interval + TimeSpan.FromSeconds(1));
        Assert.True(gate.TryPass(Evt()));
    }

    [Fact]
    public void TryPass_ExactlyAtInterval_True() {
        var clock = new FakeTimeProvider();
        var gate = new ThrottleGate("S", Interval, clock);
        gate.TryPass(Evt());
        clock.Advance(Interval);
        Assert.True(gate.TryPass(Evt()));
    }

    [Fact]
    public void TryPass_DroppedCallDoesNotExtendWindow() {
        var clock = new FakeTimeProvider();
        var gate = new ThrottleGate("S", Interval, clock);
        gate.TryPass(Evt());
        clock.Advance(TimeSpan.FromSeconds(6));
        Assert.False(gate.TryPass(Evt()));
        clock.Advance(TimeSpan.FromSeconds(4));
        Assert.True(gate.TryPass(Evt()));
    }

    [Fact]
    public void TryPass_ParallelCallsFrozenClock_ExactlyOnePasses() {
        var gate = new ThrottleGate("S", Interval, new FakeTimeProvider());
        var passed = 0;
        Parallel.For(0, 200, _ => { if (gate.TryPass(Evt())) Interlocked.Increment(ref passed); });
        Assert.Equal(1, passed);
    }

    [Fact]
    public void TryPass_IndependentInstances_DoNotShareWindow() {
        var clock = new FakeTimeProvider();
        var a = new ThrottleGate("A", Interval, clock);
        var b = new ThrottleGate("A", Interval, clock);
        Assert.True(a.TryPass(Evt()));
        Assert.True(b.TryPass(Evt()));
        Assert.False(a.TryPass(Evt()));
    }

    [Fact]
    public void TryPass_Dropped_WritesSelfLogWithSinkNameAndMessage() {
        var gate = new ThrottleGate("MySink", Interval, new FakeTimeProvider());
        var selfLog = new StringWriter();
        SelfLog.Enable(TextWriter.Synchronized(selfLog));
        try {
            gate.TryPass(Evt("first"));
            gate.TryPass(Evt("dropped-msg"));
        } finally {
            SelfLog.Disable();
        }
        var text = selfLog.ToString();
        Assert.Contains("MySink throttled: dropped-msg", text);
        Assert.DoesNotContain("first", text);
    }

    [Fact]
    public void Ctor_NullSinkName_Throws() =>
        Assert.Throws<ArgumentNullException>(() => new ThrottleGate(null!, Interval));

    [Fact]
    public void TryPass_DefaultTimeProvider_FirstPassesSecondDropped() {
        var gate = new ThrottleGate("S", TimeSpan.FromHours(1));
        Assert.True(gate.TryPass(Evt()));
        Assert.False(gate.TryPass(Evt()));
    }
}
