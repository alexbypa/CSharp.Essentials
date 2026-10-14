using CSharpEssentials.LoggerHelper.Sink.MSSqlServer;
using StandardColumn = Serilog.Sinks.MSSqlServer.StandardColumn;

namespace CSharpEssentials.LoggerHelper.Tests;

public class MSSqlServerSinkPluginTests {
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void BuildColumnOptions_StoreTimestampInUtc_MapsToConvertToUtc(bool utc) {
        var col = MSSqlServerSinkPlugin.BuildColumnOptions(new MSSqlServerSinkOptions { StoreTimestampInUtc = utc });
        Assert.Equal(utc, col.TimeStamp.ConvertToUtc);
    }

    [Fact]
    public void BuildColumnOptions_Default_ConvertToUtcIsFalse() {
        Assert.False(MSSqlServerSinkPlugin.BuildColumnOptions(new MSSqlServerSinkOptions()).TimeStamp.ConvertToUtc);
    }

    [Fact]
    public void BuildColumnOptions_AddStandardColumns_ReplacesDefaultStore() {
        var col = MSSqlServerSinkPlugin.BuildColumnOptions(new MSSqlServerSinkOptions { AddStandardColumns = ["LogEvent"] });
        Assert.Equal([StandardColumn.LogEvent], col.Store);
    }

    [Fact]
    public void BuildColumnOptions_RemoveStandardColumns_RemovesFromDefaults() {
        var col = MSSqlServerSinkPlugin.BuildColumnOptions(new MSSqlServerSinkOptions { RemoveStandardColumns = ["Exception", "bogus"] });
        Assert.DoesNotContain(StandardColumn.Exception, col.Store);
        Assert.Contains(StandardColumn.Message, col.Store);
    }
}
