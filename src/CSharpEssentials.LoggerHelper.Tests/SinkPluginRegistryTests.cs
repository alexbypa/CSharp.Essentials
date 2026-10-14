using Xunit.Abstractions;
using System.Runtime.CompilerServices;
using System.Runtime.Loader;
using CSharpEssentials.LoggerHelper.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Serilog;
using Serilog.Events;

namespace CSharpEssentials.LoggerHelper.Tests;

public class SinkPluginRegistryTests {
    private readonly ITestOutputHelper _output;

    public SinkPluginRegistryTests(ITestOutputHelper output) {
        _output = output;
    }

    [Fact]
    public void Register_AddsPluginToRegistry() {
        _output.WriteLine("[Scenario] Registry svuotato (Clear), poi Register di un plugin finto \"TestSink\"");
        _output.WriteLine("[Atteso] Il registry contiene esattamente 1 plugin");

        SinkPluginRegistry.Clear();
        SinkPluginRegistry.Register(new FakeSinkPlugin("TestSink"));

        _output.WriteLine($"[Restituito] Plugin registrati={SinkPluginRegistry.All.Count}");
        Assert.Single(SinkPluginRegistry.All);
    }

    [Fact]
    public void FindHandler_ReturnsCorrectPlugin() {
        _output.WriteLine("[Scenario] Registry svuotato e registrato un plugin che gestisce \"UniqueSink_Find\": FindHandler(\"UniqueSink_Find\")");
        _output.WriteLine("[Atteso] Viene trovato un plugin che risponde CanHandle(\"UniqueSink_Find\") == true");

        SinkPluginRegistry.Clear();
        SinkPluginRegistry.Register(new FakeSinkPlugin("UniqueSink_Find"));

        var found = SinkPluginRegistry.FindHandler("UniqueSink_Find");
        _output.WriteLine($"[Restituito] Trovato={(found is null ? "null" : found.GetType().Name)}, CanHandle={found?.CanHandle("UniqueSink_Find")}");

        Assert.NotNull(found);
        Assert.True(found.CanHandle("UniqueSink_Find"));
    }

    [Fact]
    public void FindHandler_ReturnsNull_WhenNotRegistered() {
        _output.WriteLine("[Scenario] FindHandler con un nome sink inesistente (GUID casuale)");
        _output.WriteLine("[Atteso] null: nessun plugin gestisce quel nome");

        var found = SinkPluginRegistry.FindHandler("NonExistentSink_" + Guid.NewGuid());
        _output.WriteLine($"[Restituito] Trovato={(found is null ? "null" : found.GetType().Name)}");

        Assert.Null(found);
    }

    [Fact]
    public void ConsoleSink_CanHandle_AfterManualRegistration() {
        _output.WriteLine("[Scenario] Forzo PluginInitializer.Init() del sink Console (in test il [ModuleInitializer] non è garantito) e cerco \"Console\"");
        _output.WriteLine("[Atteso] FindHandler(\"Console\") restituisce un plugin con CanHandle(\"Console\") == true");

        // [ModuleInitializer] auto-registers when assembly loads.
        // In test context, we force it by touching the type.
        CSharpEssentials.LoggerHelper.Sink.Console.PluginInitializer.Init();

        var found = SinkPluginRegistry.FindHandler("Console");
        _output.WriteLine($"[Restituito] Trovato={(found is null ? "null" : found.GetType().Name)}, CanHandle={found?.CanHandle("Console")}");

        Assert.NotNull(found);
        Assert.True(found!.CanHandle("Console"));
    }

    [Fact]
    public void FileSink_CanHandle_AfterManualRegistration() {
        _output.WriteLine("[Scenario] Forzo PluginInitializer.Init() del sink File e cerco \"File\"");
        _output.WriteLine("[Atteso] FindHandler(\"File\") restituisce un plugin con CanHandle(\"File\") == true");

        CSharpEssentials.LoggerHelper.Sink.File.PluginInitializer.Init();

        var found = SinkPluginRegistry.FindHandler("File");
        _output.WriteLine($"[Restituito] Trovato={(found is null ? "null" : found.GetType().Name)}, CanHandle={found?.CanHandle("File")}");

        Assert.NotNull(found);
        Assert.True(found!.CanHandle("File"));
    }

    public static TheoryData<string> PublishedSinks => [
        "Console", "File", "Email", "Telegram", "Elasticsearch",
        "MSSqlServer", "PostgreSQL", "MySql", "Seq", "HangfireConsole"
    ];

    [Theory]
    [MemberData(nameof(PublishedSinks))]
    public void AllPublishedSinks_Register_AndCanHandle(string sinkName) {
        _output.WriteLine($"[Scenario] Init() esplicito del plugin del sink \"{sinkName}\" (uno dei 10 sink pubblicati) e ricerca nel registry");
        _output.WriteLine($"[Atteso] FindHandler(\"{sinkName}\") restituisce un plugin con CanHandle(\"{sinkName}\") == true");

        // Explicit Init(); auto-registration is covered by the *WithoutInit tests below.
        Action init = sinkName switch {
            "Console" => CSharpEssentials.LoggerHelper.Sink.Console.PluginInitializer.Init,
            "File" => CSharpEssentials.LoggerHelper.Sink.File.PluginInitializer.Init,
            "Email" => CSharpEssentials.LoggerHelper.Sink.Email.PluginInitializer.Init,
            "Telegram" => CSharpEssentials.LoggerHelper.Sink.Telegram.PluginInitializer.Init,
            "Elasticsearch" => CSharpEssentials.LoggerHelper.Sink.Elasticsearch.PluginInitializer.Init,
            "MSSqlServer" => CSharpEssentials.LoggerHelper.Sink.MSSqlServer.PluginInitializer.Init,
            "PostgreSQL" => CSharpEssentials.LoggerHelper.Sink.Postgresql.PluginInitializer.Init,
            "MySql" => CSharpEssentials.LoggerHelper.Sink.MySql.PluginInitializer.Init,
            "Seq" => CSharpEssentials.LoggerHelper.Sink.Seq.PluginInitializer.Init,
            "HangfireConsole" => CSharpEssentials.LoggerHelper.Sink.HangfireConsole.PluginInitializer.Init,
            _ => throw new ArgumentOutOfRangeException(nameof(sinkName), sinkName, null)
        };
        init();

        var found = SinkPluginRegistry.FindHandler(sinkName);
        _output.WriteLine($"[Restituito] Trovato={(found is null ? "null" : found.GetType().Name)}, CanHandle={found?.CanHandle(sinkName)}");

        Assert.NotNull(found);
        Assert.True(found!.CanHandle(sinkName));
    }

    [Fact]
    public void AddLoggerHelper_ClearedRegistry_DiscoversAllPublishedSinksWithoutInit() {
        _output.WriteLine("[Scenario] Registry svuotato e nessun Init() manuale: AddLoggerHelper con una sola route Console (discovery all'avvio)");
        _output.WriteLine("[Atteso] Tutti i 10 sink pubblicati vengono scoperti dal registry; Console risulta caricato e configurato; nessun errore \"No plugin found\"");

        // Covers startup discovery, not the [ModuleInitializer] itself (see next test).
        SinkPluginRegistry.Clear();

        using var provider = new ServiceCollection()
            .AddLoggerHelper(b => b.AddRoute("Console", LogEventLevel.Information))
            .BuildServiceProvider();
            _output.WriteLine($"[Restituito] SinkCaricati=[{string.Join(", ", provider.GetRequiredService<ILoadedSinkStore>().GetAll().Select(s => s.SinkName + ":" + s.Configured))}], ErroriNoPlugin={provider.GetRequiredService<ILogErrorStore>().GetAll().Count(e => e.ErrorMessage.StartsWith("No plugin found"))}");

        foreach (var sinkName in PublishedSinks)
            Assert.True(SinkPluginRegistry.FindHandler(sinkName)?.CanHandle(sinkName), sinkName);
        Assert.Contains(provider.GetRequiredService<ILoadedSinkStore>().GetAll(),
            s => s.SinkName == "Console" && s.Configured);
        Assert.DoesNotContain(provider.GetRequiredService<ILogErrorStore>().GetAll(),
            e => e.ErrorMessage.StartsWith("No plugin found"));
    }

    [Fact]
    public void ModuleInitializer_RunsOnModuleLoad_RegistersPluginWithoutInit() {
        _output.WriteLine("[Scenario] Carico una copia nuova di CSharpEssentials.LoggerHelper.Sink.Console.dll in un AssemblyLoadContext separato ed eseguo il module constructor (senza Init manuale)");
        _output.WriteLine("[Atteso] Il [ModuleInitializer] si esegue al caricamento del modulo e registra un plugin Console proveniente da quell'assembly");

        SinkPluginRegistry.Clear();
        // The Default ALC already ran Console's initializer once per process: load a fresh copy instead.
        // Non-collectible: the static registry keeps a reference to the plugin anyway.
        var alc = new AssemblyLoadContext("A11-ModuleInitializer", isCollectible: false);
        var asm = alc.LoadFromAssemblyPath(
            Path.Combine(AppContext.BaseDirectory, "CSharpEssentials.LoggerHelper.Sink.Console.dll"));

        try {
            RuntimeHelpers.RunModuleConstructor(asm.ManifestModule.ModuleHandle);
            _output.WriteLine($"[Restituito] PluginDaAssemblyCaricato={SinkPluginRegistry.All.Count(p => p.GetType().Assembly == asm)}");

            Assert.Contains(SinkPluginRegistry.All,
                p => p.GetType().Assembly == asm && p.CanHandle("Console"));
        } finally {
            SinkPluginRegistry.Clear(); // don't leak the foreign-ALC plugin to later tests
        }
    }

    private sealed class FakeSinkPlugin : ISinkPlugin {
        private readonly string _name;

        internal FakeSinkPlugin(string name) => _name = name;

        public bool CanHandle(string sinkName) => sinkName == _name;
        public void Configure(LoggerConfiguration loggerConfig, SinkRouting routing, LoggerHelperOptions options) { }
    }
}
