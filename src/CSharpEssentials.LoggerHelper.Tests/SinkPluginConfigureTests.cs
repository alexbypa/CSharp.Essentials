using Xunit.Abstractions;
using CSharpEssentials.LoggerHelper.Sink.Elasticsearch;
using CSharpEssentials.LoggerHelper.Sink.Email;
using CSharpEssentials.LoggerHelper.Sink.HangfireConsole;
using CSharpEssentials.LoggerHelper.Sink.MSSqlServer;
using CSharpEssentials.LoggerHelper.Sink.MySql;
using CSharpEssentials.LoggerHelper.Sink.Postgresql;
using CSharpEssentials.LoggerHelper.Sink.Seq;
using CSharpEssentials.LoggerHelper.Sink.Telegram;
using Microsoft.Extensions.DependencyInjection;
using CSharpEssentials.LoggerHelper.Diagnostics;
using Microsoft.Extensions.Configuration;
using Serilog;
using Serilog.Debugging;

namespace CSharpEssentials.LoggerHelper.Tests;

public class SinkPluginConfigureTests {
    private readonly ITestOutputHelper _output;

    public SinkPluginConfigureTests(ITestOutputHelper output) {
        _output = output;
    }

    private const string MissingConfigMessage = "sink configured in routes but";

    private static readonly string[] AllSinks =
        ["Email", "MSSqlServer", "Postgresql", "MySql", "Telegram", "HangfireConsole", "Seq", "Elasticsearch"];

    public static TheoryData<string> Sinks() {
        var data = new TheoryData<string>();
        foreach (var s in AllSinks) data.Add(s);
        return data;
    }

    [Theory]
    [MemberData(nameof(Sinks))]
    public void Configure_MissingConfig_ThrowsInvalidOperationException(string sink) {
        _output.WriteLine($"[Scenario] Sink \"{sink}\" referenziato nelle Routes ma senza alcuna configurazione (nessun SetSinkConfig / sezione JSON)");
        _output.WriteLine("[Atteso] InvalidOperationException con messaggio \"sink configured in routes but ...\"");

        var options = new LoggerHelperOptions();
        var plugin = CreatePlugin(sink);
        var previousAccessor = HangfireConsoleSinkAccessorHolder.Accessor;

        try {
            HangfireConsoleSinkAccessorHolder.Accessor = null;
            var ex = Assert.Throws<InvalidOperationException>(() =>
                plugin.Configure(new LoggerConfiguration(), new SinkRouting { Sink = sink, Levels = ["Error"] }, options));

            _output.WriteLine($"[Restituito] {ex.GetType().Name}: {ex.Message}");
            Assert.Contains(MissingConfigMessage, ex.Message);
        } finally {
            HangfireConsoleSinkAccessorHolder.Accessor = previousAccessor;
        }
    }

    [Theory]
    [InlineData("Email", "Host")]
    [InlineData("Email", "From")]
    [InlineData("Email", "To")]
    [InlineData("MSSqlServer", "ConnectionString")]
    [InlineData("Postgresql", "ConnectionString")]
    [InlineData("MySql", "ConnectionString")]
    [InlineData("Telegram", "BotToken")]
    [InlineData("Telegram", "ChatId")]
    [InlineData("Seq", "ServerUrl")]
    [InlineData("Elasticsearch", "NodeUris")]
    public void Configure_RequiredFieldEmpty_Throws(string sink, string field) {
        _output.WriteLine($"[Scenario] Sink \"{sink}\" con configurazione valida ma il campo obbligatorio \"{field}\" è vuoto");
        _output.WriteLine($"[Atteso] InvalidOperationException con messaggio \"{field} is required\"");

        var options = new LoggerHelperOptions();
        ApplyMinimalConfig(sink, options, blank: field);

        var ex = Assert.Throws<InvalidOperationException>(() =>
            CreatePlugin(sink).Configure(new LoggerConfiguration(), new SinkRouting { Sink = sink, Levels = ["Error"] }, options));

        _output.WriteLine($"[Restituito] {ex.GetType().Name}: {ex.Message}");
        Assert.Contains($"{field} is required", ex.Message);
    }

    [Theory]
    [MemberData(nameof(Sinks))]
    public void Configure_ValidConfig_SinkIsAddedAndMarkedConfigured(string sink) {
        _output.WriteLine($"[Scenario] Sink \"{sink}\" con configurazione minima valida (endpoint non raggiungibili, nessun evento emesso) passato al SinkRoutingEngine");
        _output.WriteLine("[Atteso] Nessun errore registrato in LogErrorStore e un solo sink in LoadedSinkStore con Configured == true");

        var options = new LoggerHelperOptions { Routes = [new SinkRouting { Sink = sink, Levels = ["Error"] }] };
        var previousAccessor = HangfireConsoleSinkAccessorHolder.Accessor;
        var errors = new LogErrorStore();
        var loaded = new LoadedSinkStore();

        try {
            ApplyMinimalConfig(sink, options);
            var engine = new SinkRoutingEngine(options, errors, loaded, new SingleRegistry(CreatePlugin(sink)), new NoDiscovery());
            var cfg = new LoggerConfiguration();

            engine.ConfigureRoutes(cfg);
            using (cfg.CreateLogger()) { }
        } finally {
            HangfireConsoleSinkAccessorHolder.Accessor = previousAccessor;
        }

        _output.WriteLine($"[Restituito] Errori={errors.GetAll().Count}, SinkCaricati={loaded.GetAll().Count}, Configured={loaded.GetAll().FirstOrDefault()?.Configured}");
        Assert.Empty(errors.GetAll());
        Assert.True(Assert.Single(loaded.GetAll()).Configured);
    }

    [Fact]
    public void Emit_NoPerformContext_DoesNothing() {
        _output.WriteLine("[Scenario] HangfireConsoleSerilogSink con PerformContextAccessor nuovo (Current == null, nessun job Hangfire in esecuzione) e un LogEvent Information");
        _output.WriteLine("[Atteso] Emit ignora l'evento senza lanciare eccezioni");

        var accessor = new PerformContextAccessor();
        var sink = new HangfireConsoleSerilogSink(accessor, new CSharpEssentials.LoggerHelper.Diagnostics.ThrottleGate("HangfireConsole", null));
        var evt = new Serilog.Events.LogEvent(DateTimeOffset.UtcNow, Serilog.Events.LogEventLevel.Information, null,
            new Serilog.Parsing.MessageTemplateParser().Parse("hello"), []);

        var ex = Record.Exception(() => sink.Emit(evt));

        _output.WriteLine($"[Restituito] Current={(accessor.Current is null ? "null" : "set")}, Eccezione={ex?.GetType().Name ?? "nessuna"}");
        Assert.Null(accessor.Current);
        Assert.Null(ex);
    }

    [Fact]
    public void Engine_PluginThrows_RecordsFailedSinkAndError() {
        _output.WriteLine("[Scenario] Route verso Seq senza configurazione: il plugin lancia durante ConfigureRoutes");
        _output.WriteLine("[Atteso] Nessuna eccezione propagata; Seq registrato come non configurato (Configured=false) e errore \"Seq sink configured in routes but...\" in LogErrorStore");

        var options = new LoggerHelperOptions { Routes = [new SinkRouting { Sink = "Seq", Levels = ["Error"] }] };
        var errors = new LogErrorStore();
        var loaded = new LoadedSinkStore();
        var engine = new SinkRoutingEngine(options, errors, loaded, new SingleRegistry(new SeqSinkPlugin()), new NoDiscovery());

        engine.ConfigureRoutes(new LoggerConfiguration());

        var info = Assert.Single(loaded.GetAll());
        _output.WriteLine($"[Restituito] Sink={info.SinkName}, Configured={info.Configured}, Errori=[{string.Join(" | ", errors.GetAll().Select(e => e.ErrorMessage))}]");
        Assert.Equal("Seq", info.SinkName);
        Assert.False(info.Configured);
        Assert.Contains(errors.GetAll(), e => e.SinkName == "Seq" && e.ErrorMessage.Contains("Seq sink configured in routes but"));
    }

    [Fact]
    public void ElasticsearchOptions_AutoRegisterTemplate_DefaultsToTrue() {
        _output.WriteLine("[Scenario] Nuova istanza di ElasticsearchSinkOptions senza valori impostati");
        _output.WriteLine("[Atteso] AutoRegisterTemplate == true (default)");

        var value = new ElasticsearchSinkOptions().AutoRegisterTemplate;

        _output.WriteLine($"[Restituito] AutoRegisterTemplate={value}");
        Assert.True(value);
    }

    [Fact]
    public void ElasticsearchOptions_LegacySetter_NullKeepsValueFalseDisables() {
        _output.WriteLine("[Scenario] Setter legacy autoRegisterTemplate: prima impostato a null, poi a false");
        _output.WriteLine("[Atteso] null mantiene il default (AutoRegisterTemplate=true); false lo disabilita (AutoRegisterTemplate=false)");

        var o = new ElasticsearchSinkOptions { autoRegisterTemplate = null };
        var afterNull = o.AutoRegisterTemplate;
        o.autoRegisterTemplate = false;
        var afterFalse = o.AutoRegisterTemplate;

        _output.WriteLine($"[Restituito] dopo null={afterNull}, dopo false={afterFalse}");
        Assert.True(afterNull);
        Assert.False(afterFalse);
    }

    [Fact]
    public void ElasticsearchOptions_LegacyAutoRegisterTemplateKey_BindsFromConfiguration() {
        _output.WriteLine("[Scenario] Configurazione JSON con chiavi legacy minuscole (Sinks:Elasticsearch:nodeUris e autoRegisterTemplate=false) letta con BindSinkSection");
        _output.WriteLine("[Atteso] Opzioni valorizzate: NodeUris=\"http://127.0.0.1:1\" e AutoRegisterTemplate=false");

        var config = new Microsoft.Extensions.Configuration.ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> {
                ["Sinks:Elasticsearch:nodeUris"] = "http://127.0.0.1:1",
                ["Sinks:Elasticsearch:autoRegisterTemplate"] = "false"
            }).Build();
        var options = new LoggerHelperOptions { RawSinksSection = config.GetSection("Sinks") };

        var bound = options.BindSinkSection<ElasticsearchSinkOptions>("Elasticsearch");

        _output.WriteLine($"[Restituito] Bound={(bound is null ? "null" : "ok")}, NodeUris={bound?.NodeUris}, AutoRegisterTemplate={bound?.AutoRegisterTemplate}");
        Assert.NotNull(bound);
        Assert.False(bound!.AutoRegisterTemplate);
        Assert.Equal("http://127.0.0.1:1", bound.NodeUris);
    }

    private sealed class SingleRegistry(ISinkPlugin plugin) : ISinkPluginRegistry {
        public ISinkPlugin? FindHandler(string sinkName) => plugin.CanHandle(sinkName) ? plugin : null;
        public IReadOnlyCollection<ISinkPlugin> All => [plugin];
    }

    private sealed class NoDiscovery : IPluginDiscovery {
        public void DiscoverAndLoad(ILogErrorStore errorStore) { }
    }

    private static ISinkPlugin CreatePlugin(string sink) => sink switch {
        "Email" => new EmailSinkPlugin(),
        "MSSqlServer" => new MSSqlServerSinkPlugin(),
        "Postgresql" => new PostgreSqlSinkPlugin(),
        "MySql" => new MySqlSinkPlugin(),
        "Telegram" => new TelegramSinkPlugin(),
        "HangfireConsole" => new HangfireConsoleSinkPlugin(),
        "Seq" => new SeqSinkPlugin(),
        "Elasticsearch" => new ElasticsearchSinkPlugin(),
        _ => throw new ArgumentOutOfRangeException(nameof(sink), sink, null)
    };

    // Unreachable endpoints (127.0.0.1:1) and no emitted events: nothing should touch the network.
    private static void ApplyMinimalConfig(string sink, LoggerHelperOptions options, string? blank = null) {
        string V(string field, string value) => field == blank ? string.Empty : value;
        switch (sink) {
            case "Email":
                options.SetSinkConfig("Email", new EmailSinkOptions { Host = V("Host", "localhost"), From = V("From", "a@b.c"), To = V("To", "d@e.f") });
                break;
            case "MSSqlServer":
                options.SetSinkConfig("MSSqlServer", new MSSqlServerSinkOptions {
                    ConnectionString = V("ConnectionString", "Server=127.0.0.1,1;Database=x;User Id=x;Password=x;Connect Timeout=1"),
                    AutoCreateSqlTable = false
                });
                break;
            case "Postgresql":
                options.SetSinkConfig("PostgreSql", new PostgreSqlSinkOptions {
                    ConnectionString = V("ConnectionString", "Host=127.0.0.1;Port=1;Database=x;Username=x;Password=x;Timeout=1"),
                    NeedAutoCreateTable = false
                });
                break;
            case "MySql":
                options.SetSinkConfig("MySql", new MySqlSinkOptions {
                    ConnectionString = V("ConnectionString", "Server=127.0.0.1;Port=1;Database=x;Uid=x;Pwd=x;Connection Timeout=1"),
                    AutoCreateTable = false
                });
                break;
            case "Telegram":
                options.SetSinkConfig("Telegram", new TelegramSinkOptions { BotToken = V("BotToken", "x"), ChatId = V("ChatId", "1") });
                break;
            case "Seq":
                options.SetSinkConfig("Seq", new SeqSinkOptions { ServerUrl = V("ServerUrl", "http://127.0.0.1:1") });
                break;
            case "Elasticsearch":
                options.SetSinkConfig("Elasticsearch", new ElasticsearchSinkOptions { NodeUris = V("NodeUris", "http://127.0.0.1:1") });
                break;
            case "HangfireConsole":
                new ServiceCollection().AddHangfireConsoleSink();
                break;
        }
    }
}




