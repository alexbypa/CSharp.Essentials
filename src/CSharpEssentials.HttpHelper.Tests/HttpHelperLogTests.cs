using CSharpEssentials.LoggerHelper;
using Microsoft.Extensions.DependencyInjection;
using Serilog;
using Serilog.Core;
using Serilog.Events;
using Xunit.Abstractions;

namespace CSharpEssentials.HttpHelper.Tests;

[CollectionDefinition(Name, DisableParallelization = true)]
public sealed class HttpHelperLogCollection {
    public const string Name = "HttpHelperLog (static logger state)";
}

[Collection(HttpHelperLogCollection.Name)]
public class HttpHelperLogTests {
    private readonly ITestOutputHelper _output;

    public HttpHelperLogTests(ITestOutputHelper output) {
        _output = output;
    }

    private sealed class CaptureSink : ILogEventSink {
        public List<LogEvent> Events { get; } = [];
        public void Emit(LogEvent logEvent) => Events.Add(logEvent);
    }

    private static void ApplyInvalidProxy() {
        using var handler = new SocketsHttpHandler();
        ProxyConfigurator.Apply(handler, new httpClientOptions {
            Name = "A",
            httpProxy = new httpProxy { Address = "not a valid uri", UseProxy = true }
        });
    }

    [Fact]
    public void Write_WithLoggerHelperConfigured_ReachesLoggerHelperPipeline() {
        _output.WriteLine("[Scenario] AddLoggerHelper con sink di cattura, poi ProxyConfigurator.Apply con indirizzo proxy non valido");
        _output.WriteLine("[Atteso] L'errore interno di HttpHelper arriva al sink di LoggerHelper con Action=HttpHelper");

        var capture = new CaptureSink();
        var previous = Log.Logger;
        var services = new ServiceCollection();
        services.AddLoggerHelper(b => b.WithApplicationName("T").WithEnrichers(c => c.WriteTo.Sink(capture)));
        using var provider = services.BuildServiceProvider();
        try {
            ApplyInvalidProxy();
        } finally {
            (provider.GetRequiredService<Serilog.ILogger>() as IDisposable)?.Dispose();
            Log.Logger = previous;
        }

        var evt = Assert.Single(capture.Events, e => e.Level == LogEventLevel.Error);
        _output.WriteLine($"[Restituito] Eventi={capture.Events.Count}, Action={evt.Properties["Action"]}");
        Assert.NotNull(evt.Exception);
        Assert.Equal("\"HttpHelper\"", evt.Properties["Action"].ToString());
    }

    [Fact]
    public void Write_WithoutLoggerHelper_FallsBackToStaticLog() {
        _output.WriteLine("[Scenario] Nessun AddLoggerHelper attivo, Log.Logger statico con sink di cattura, ProxyConfigurator.Apply con indirizzo non valido");
        _output.WriteLine("[Atteso] L'errore interno di HttpHelper arriva a Log.Logger");

        var capture = new CaptureSink();
        var previous = Log.Logger;
        Log.Logger = new LoggerConfiguration().WriteTo.Sink(capture).CreateLogger();
        try {
            ApplyInvalidProxy();
        } finally {
            Log.Logger = previous;
        }

        _output.WriteLine($"[Restituito] Eventi={capture.Events.Count}");
        Assert.Contains(capture.Events, e => e.Level == LogEventLevel.Error && e.Exception is not null);
    }
}
