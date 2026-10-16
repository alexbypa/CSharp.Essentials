using CSharpEssentials.LoggerHelper.Diagnostics;
using CSharpEssentials.LoggerHelper.Sink.Console;
using CSharpEssentials.LoggerHelper.Sink.Elasticsearch;
using CSharpEssentials.LoggerHelper.Sink.Email;
using CSharpEssentials.LoggerHelper.Sink.File;
using CSharpEssentials.LoggerHelper.Sink.HangfireConsole;
using CSharpEssentials.LoggerHelper.Sink.MSSqlServer;
using CSharpEssentials.LoggerHelper.Sink.MySql;
using CSharpEssentials.LoggerHelper.Sink.Postgresql;
using CSharpEssentials.LoggerHelper.Sink.Seq;
using CSharpEssentials.LoggerHelper.Sink.Telegram;
using Microsoft.Extensions.Configuration;
using Serilog;
using Serilog.Debugging;
using Serilog.Events;
using Serilog.Parsing;

namespace CSharpEssentials.LoggerHelper.Tests;

public class SinkThrottleIntegrationTests {
    private sealed class StubGate(bool result) : IThrottleGate {
        public int Calls;
        public bool TryPass(LogEvent logEvent) { Calls++; return result; }
    }

    private static LogEvent Evt(string message = "hello") =>
        new(DateTimeOffset.UtcNow, LogEventLevel.Error, null, new MessageTemplateParser().Parse(message), []);

    private static string CaptureSelfLog(Action act) {
        var selfLog = new StringWriter();
        SelfLog.Enable(TextWriter.Synchronized(selfLog));
        try { act(); } finally { SelfLog.Disable(); }
        return selfLog.ToString();
    }

    private static TimeSpan? BindInterval<T>(string sink, Func<T, TimeSpan?> read) where T : class, new() {
        var cfg = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?> {
            [$"Sinks:{sink}:ThrottleInterval"] = "00:00:05"
        }).Build();
        var options = new LoggerHelperOptions { RawSinksSection = cfg.GetSection("Sinks") };
        return read(options.BindSinkSection<T>(sink)!);
    }

    public static TheoryData<string> SinkNames => new() {
        "Console", "File", "Email", "Telegram", "Elasticsearch", "MSSqlServer", "Postgresql", "MySql", "Seq", "HangfireConsole"
    };

    [Theory]
    [MemberData(nameof(SinkNames))]
    public void BindSinkSection_ThrottleIntervalString_BindsToTimeSpan(string sink) {
        var bound = sink switch {
            "Console" => BindInterval<ConsoleSinkOptions>(sink, o => o.ThrottleInterval),
            "File" => BindInterval<FileSinkOptions>(sink, o => o.ThrottleInterval),
            "Email" => BindInterval<EmailSinkOptions>(sink, o => o.ThrottleInterval),
            "Telegram" => BindInterval<TelegramSinkOptions>(sink, o => o.ThrottleInterval),
            "Elasticsearch" => BindInterval<ElasticsearchSinkOptions>(sink, o => o.ThrottleInterval),
            "MSSqlServer" => BindInterval<MSSqlServerSinkOptions>(sink, o => o.ThrottleInterval),
            "Postgresql" => BindInterval<PostgreSqlSinkOptions>(sink, o => o.ThrottleInterval),
            "MySql" => BindInterval<MySqlSinkOptions>(sink, o => o.ThrottleInterval),
            "Seq" => BindInterval<SeqSinkOptions>(sink, o => o.ThrottleInterval),
            _ => BindInterval<HangfireConsoleSinkOptions>(sink, o => o.ThrottleInterval),
        };
        Assert.Equal(TimeSpan.FromSeconds(5), bound);
    }

    [Fact]
    public void GetSinkConfig_FluentThrottleInterval_RoundTrips() {
        var options = new LoggerHelperOptions();
        options.SetSinkConfig("Console", new ConsoleSinkOptions { ThrottleInterval = TimeSpan.FromSeconds(7) });
        Assert.Equal(TimeSpan.FromSeconds(7), options.GetSinkConfig<ConsoleSinkOptions>("Console")!.ThrottleInterval);
    }

    [Fact]
    public void Console_ThrottleInterval_SecondEventDroppedAndSelfLogged() {
        var options = new LoggerHelperOptions();
        options.SetSinkConfig("Console", new ConsoleSinkOptions { OutputTemplate = "{Message}", ThrottleInterval = TimeSpan.FromHours(1) });
        var config = new LoggerConfiguration().MinimumLevel.Verbose();
        new ConsoleSinkPlugin().Configure(config, new SinkRouting { Sink = "Console", Levels = ["Information"] }, options);

        var original = Console.Out;
        var sw = new StringWriter();
        Console.SetOut(sw);
        string selfLog;
        try {
            selfLog = CaptureSelfLog(() => {
                using var logger = config.CreateLogger();
                logger.Information("one");
                logger.Information("two");
            });
        } finally {
            Console.SetOut(original);
        }

        Assert.Contains("one", sw.ToString());
        Assert.DoesNotContain("two", sw.ToString());
        Assert.Contains("Console throttled: two", selfLog);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("TenantId")]
    public void File_ThrottleInterval_SecondEventDropped(string? fileNameProperty) {
        var dir = Path.Combine(Path.GetTempPath(), "lh-throttle-" + Guid.NewGuid().ToString("N"));
        try {
            var options = new LoggerHelperOptions();
            options.SetSinkConfig("File", new FileSinkOptions { Path = dir, FileNameProperty = fileNameProperty, ThrottleInterval = TimeSpan.FromHours(1) });
            var config = new LoggerConfiguration().MinimumLevel.Verbose();
            new FileSinkPlugin().Configure(config, new SinkRouting { Sink = "File", Levels = ["Error"] }, options);
            using (var logger = config.CreateLogger()) {
                logger.Error("first-event");
                logger.Error("second-event");
            }

            var content = string.Concat(Directory.GetFiles(dir, "*", SearchOption.AllDirectories).Select(f => {
                using var fs = new FileStream(f, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                return new StreamReader(fs).ReadToEnd();
            }));
            Assert.Contains("first-event", content);
            Assert.DoesNotContain("second-event", content);
        } finally {
            try { Directory.Delete(dir, true); } catch (IOException) { }
        }
    }

    [Fact]
    public void File_NonMatchingLevel_DoesNotConsumeThrottleSlot() {
        var dir = Path.Combine(Path.GetTempPath(), "lh-throttle-" + Guid.NewGuid().ToString("N"));
        try {
            var options = new LoggerHelperOptions();
            options.SetSinkConfig("File", new FileSinkOptions { Path = dir, ThrottleInterval = TimeSpan.FromHours(1) });
            var config = new LoggerConfiguration().MinimumLevel.Verbose();
            new FileSinkPlugin().Configure(config, new SinkRouting { Sink = "File", Levels = ["Error"] }, options);
            using (var logger = config.CreateLogger()) {
                logger.Information("ignored-by-route");
                logger.Error("first-error");
            }

            var content = string.Concat(Directory.GetFiles(dir, "*", SearchOption.AllDirectories).Select(f => {
                using var fs = new FileStream(f, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                return new StreamReader(fs).ReadToEnd();
            }));
            Assert.DoesNotContain("ignored-by-route", content);
            Assert.Contains("first-error", content);
        } finally {
            try { Directory.Delete(dir, true); } catch (IOException) { }
        }
    }

    [Fact]
    public void Email_GateRejects_SendSkipped() {
        var opts = new EmailSinkOptions { From = "a@b.c", To = "d@e.f", Host = "127.0.0.1", Port = 1, EnableSsl = false };
        var gate = new StubGate(false);
        using var sink = new EmailLogEventSink(opts, gate);

        var selfLog = CaptureSelfLog(() => sink.Emit(Evt()));

        Assert.Equal(1, gate.Calls);
        Assert.DoesNotContain("Error sending email", selfLog);
    }

    [Fact]
    public void Email_GatePasses_ReachesSendPath() {
        var opts = new EmailSinkOptions { From = "a@b.c", To = "d@e.f", Host = "127.0.0.1", Port = 1, EnableSsl = false };
        var gate = new StubGate(true);
        using var sink = new EmailLogEventSink(opts, gate);

        var selfLog = CaptureSelfLog(() => sink.Emit(Evt()));

        Assert.Equal(1, gate.Calls);
        Assert.Contains("Error sending email", selfLog);
    }

    [Fact]
    public void Telegram_GateRejects_GateCalledOnceNoSend() {
        var gate = new StubGate(false);
        var sink = new TelegramLogEventSink(new TelegramSinkOptions { BotToken = "t", ChatId = "c" }, gate);

        var ex = Record.Exception(() => sink.Emit(Evt()));

        Assert.Null(ex);
        Assert.Equal(1, gate.Calls);
    }

    [Fact]
    public void HangfireConsole_NoPerformContext_GateNotCalled() {
        var gate = new StubGate(true);
        var sink = new HangfireConsoleSerilogSink(new PerformContextAccessor(), gate);

        sink.Emit(Evt());

        Assert.Equal(0, gate.Calls);
    }
}
