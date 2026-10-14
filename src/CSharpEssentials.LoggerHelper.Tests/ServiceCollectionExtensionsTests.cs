using Xunit.Abstractions;
using CSharpEssentials.LoggerHelper.Diagnostics;
using CSharpEssentials.LoggerHelper.Sink.Seq;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Serilog.Events;

namespace CSharpEssentials.LoggerHelper.Tests;

public class ServiceCollectionExtensionsTests {
    private readonly ITestOutputHelper _output;

    public ServiceCollectionExtensionsTests(ITestOutputHelper output) {
        _output = output;
    }

    [Fact]
    public void AddLoggerHelper_Fluent_RegistersServices() {
        _output.WriteLine("[Scenario] AddLoggerHelper(fluent) con ApplicationName \"TestApp\" e route Console/Information: costruisco il ServiceProvider");
        _output.WriteLine("[Atteso] Sono registrati LoggerHelperOptions, ILogErrorStore, LogErrorStore, ISinkPluginRegistry, Serilog.ILogger e ILoggerProvider");

        var services = new ServiceCollection();

        services.AddLoggerHelper(b => b
            .WithApplicationName("TestApp")
            .AddRoute("Console", LogEventLevel.Information)
        );

        var provider = services.BuildServiceProvider();
        _output.WriteLine($"[Restituito] Options={provider.GetService<LoggerHelperOptions>() is not null}, ILogErrorStore={provider.GetService<ILogErrorStore>() is not null}, LogErrorStore={provider.GetService<LogErrorStore>() is not null}, ISinkPluginRegistry={provider.GetService<ISinkPluginRegistry>() is not null}, Serilog.ILogger={provider.GetService<Serilog.ILogger>() is not null}, ILoggerProvider={provider.GetService<ILoggerProvider>() is not null}");

        Assert.NotNull(provider.GetService<LoggerHelperOptions>());
        Assert.NotNull(provider.GetService<ILogErrorStore>());
        Assert.NotNull(provider.GetService<LogErrorStore>());
        Assert.NotNull(provider.GetService<ISinkPluginRegistry>());
        Assert.NotNull(provider.GetService<Serilog.ILogger>());
        Assert.NotNull(provider.GetService<ILoggerProvider>());
    }

    [Fact]
    public void AddLoggerHelper_SetsApplicationName() {
        _output.WriteLine("[Scenario] AddLoggerHelper(fluent) con WithApplicationName(\"MyTestApp\")");
        _output.WriteLine("[Atteso] LoggerHelperOptions.ApplicationName == \"MyTestApp\"");

        var services = new ServiceCollection();

        services.AddLoggerHelper(b => b
            .WithApplicationName("MyTestApp")
            .AddRoute("Console", LogEventLevel.Information)
        );

        var provider = services.BuildServiceProvider();
        var options = provider.GetRequiredService<LoggerHelperOptions>();
        _output.WriteLine($"[Restituito] ApplicationName={options.ApplicationName}");

        Assert.Equal("MyTestApp", options.ApplicationName);
    }

    [Fact]
    public void AddLoggerHelper_ILoggerProvider_CreatesLoggers() {
        _output.WriteLine("[Scenario] AddLoggerHelper con route Console/Information: risolvo ILoggerProvider e creo un logger per la categoria \"TestCategory\"");
        _output.WriteLine("[Atteso] Il logger è creato (non null) e abilitato per LogLevel.Information");

        var services = new ServiceCollection();

        services.AddLoggerHelper(b => b
            .WithApplicationName("ILoggerTest")
            .AddRoute("Console", LogEventLevel.Information)
        );

        var provider = services.BuildServiceProvider();
        var loggerProvider = provider.GetRequiredService<ILoggerProvider>();
        var logger = loggerProvider.CreateLogger("TestCategory");
        _output.WriteLine($"[Restituito] Logger={(logger is null ? "null" : logger.GetType().Name)}, IsEnabled(Information)={logger?.IsEnabled(LogLevel.Information)}");

        Assert.NotNull(logger);
        Assert.True(logger.IsEnabled(LogLevel.Information));
    }

    [Fact]
    public void AddLoggerHelper_WithMultipleRoutes_ConfiguresAll() {
        _output.WriteLine("[Scenario] AddLoggerHelper con due route: Console (Information, Warning) e File (Error, Fatal)");
        _output.WriteLine("[Atteso] Options.Routes contiene 2 route nell'ordine Console, File");

        var services = new ServiceCollection();

        services.AddLoggerHelper(b => b
            .WithApplicationName("MultiRoute")
            .AddRoute("Console", LogEventLevel.Information, LogEventLevel.Warning)
            .AddRoute("File", LogEventLevel.Error, LogEventLevel.Fatal)
        );

        var provider = services.BuildServiceProvider();
        var options = provider.GetRequiredService<LoggerHelperOptions>();
        _output.WriteLine($"[Restituito] Routes=[{string.Join(", ", options.Routes.Select(r => r.Sink))}]");

        Assert.Equal(2, options.Routes.Count);
        Assert.Equal("Console", options.Routes[0].Sink);
        Assert.Equal("File", options.Routes[1].Sink);
    }

    [Fact]
    public void AddLoggerHelper_EnableSelfLogging_SetsFlag() {
        _output.WriteLine("[Scenario] AddLoggerHelper con EnableSelfLogging() nel fluent builder");
        _output.WriteLine("[Atteso] Options.General.EnableSelfLogging == true");

        var services = new ServiceCollection();

        services.AddLoggerHelper(b => b
            .WithApplicationName("SelfLogTest")
            .AddRoute("Console", LogEventLevel.Information)
            .EnableSelfLogging()
        );

        var provider = services.BuildServiceProvider();
        var options = provider.GetRequiredService<LoggerHelperOptions>();
        _output.WriteLine($"[Restituito] EnableSelfLogging={options.General.EnableSelfLogging}");

        Assert.True(options.General.EnableSelfLogging);
    }

    [Fact]
    public void AddLoggerHelper_ErrorStore_IsRegistered() {
        _output.WriteLine("[Scenario] AddLoggerHelper: risolvo sia l'interfaccia ILogErrorStore sia il tipo concreto LogErrorStore");
        _output.WriteLine("[Atteso] Entrambe le risoluzioni riescono (non null)");

        var services = new ServiceCollection();

        services.AddLoggerHelper(b => b
            .WithApplicationName("ErrorStoreTest")
            .AddRoute("Console", LogEventLevel.Information)
        );

        var provider = services.BuildServiceProvider();

        // Both interface and concrete type should resolve
        var errorStore = provider.GetRequiredService<ILogErrorStore>();
        var concreteStore = provider.GetRequiredService<LogErrorStore>();
        _output.WriteLine($"[Restituito] ILogErrorStore={errorStore.GetType().Name}, LogErrorStore={concreteStore.GetType().Name}");

        Assert.NotNull(errorStore);
        Assert.NotNull(concreteStore);
    }

    private static IConfiguration InMemory(params (string Key, string? Value)[] values) =>
        new ConfigurationBuilder()
            .AddInMemoryCollection(values.Select(v => new KeyValuePair<string, string?>(v.Key, v.Value)))
            .Build();

    private static readonly (string, string?)[] ConsoleRoute = [
        ("LoggerHelper:ApplicationName", "JsonApp"),
        ("LoggerHelper:Routes:0:Sink", "Console"),
        ("LoggerHelper:Routes:0:Levels:0", "Information"),
    ];

    private const string JsonFileContent = """
        { "LoggerHelper": { "ApplicationName": "FileApp",
          "Routes": [ { "Sink": "Console", "Levels": [ "Information" ] } ] } }
        """;

    [Fact]
    public void AddLoggerHelper_ConfigurationWithoutRoutes_ThrowsInvalidOperationException() {
        _output.WriteLine("[Scenario] AddLoggerHelper(IConfiguration) con la sola sezione LoggerHelper:ApplicationName e nessuna route (ambiente isolato, nessun file JSON)");
        _output.WriteLine("[Atteso] InvalidOperationException con messaggio \"No routes configured\"");

        using var _ = new IsolatedEnvironment();
        var services = new ServiceCollection();

        var ex = Assert.Throws<InvalidOperationException>(() =>
            services.AddLoggerHelper(InMemory(("LoggerHelper:ApplicationName", "NoRoutes"))));
        _output.WriteLine($"[Restituito] {ex.GetType().Name}: {ex.Message}");

        Assert.Contains("No routes configured", ex.Message);
    }

    [Fact]
    public void AddLoggerHelper_ConfigurationWithRoutes_BindsOptionsAndRawSinksSection() {
        _output.WriteLine("[Scenario] AddLoggerHelper(IConfiguration) con ApplicationName \"BoundApp\", route Console/Information e Sinks:Console:OutputTemplate");
        _output.WriteLine("[Atteso] Opzioni valorizzate dal binding: ApplicationName \"BoundApp\", una route Console con livello Information, RawSinksSection presente con la sezione \"Console\"");

        using var _ = new IsolatedEnvironment();
        var services = new ServiceCollection();

        services.AddLoggerHelper(InMemory(
            ("LoggerHelper:ApplicationName", "BoundApp"),
            ("LoggerHelper:Routes:0:Sink", "Console"),
            ("LoggerHelper:Routes:0:Levels:0", "Information"),
            ("LoggerHelper:Sinks:Console:OutputTemplate", "{Message}")));

        using var provider = services.BuildServiceProvider();
        var options = provider.GetRequiredService<LoggerHelperOptions>();
        _output.WriteLine($"[Restituito] ApplicationName={options.ApplicationName}, Routes=[{string.Join(", ", options.Routes.Select(r => r.Sink + ":" + string.Join("/", r.Levels)))}], RawSinksSection={(options.RawSinksSection is null ? "null" : "presente")}, SezioneConsole={options.RawSinksSection?.GetSection("Console").Exists()}");

        Assert.Equal("BoundApp", options.ApplicationName);
        Assert.Single(options.Routes);
        Assert.Equal("Console", options.Routes[0].Sink);
        Assert.Equal("Information", Assert.Single(options.Routes[0].Levels));
        Assert.NotNull(options.RawSinksSection);
        Assert.True(options.RawSinksSection!.GetSection("Console").Exists());
    }

    [Fact]
    public void AddLoggerHelper_LegacySerilogConfiguration_FallsBackToLegacyRoutes() {
        _output.WriteLine("[Scenario] Configurazione senza sezione LoggerHelper ma con la sezione legacy Serilog:SerilogConfiguration (ApplicationName \"LegacyApp\", una SerilogCondition Console/Information)");
        _output.WriteLine("[Atteso] Fallback al formato legacy: ApplicationName \"LegacyApp\" e una route Console");

        using var _ = new IsolatedEnvironment();
        var services = new ServiceCollection();

        services.AddLoggerHelper(InMemory(
            ("Serilog:SerilogConfiguration:ApplicationName", "LegacyApp"),
            ("Serilog:SerilogConfiguration:SerilogCondition:0:Sink", "Console"),
            ("Serilog:SerilogConfiguration:SerilogCondition:0:Level:0", "Information")));

        using var provider = services.BuildServiceProvider();
        var options = provider.GetRequiredService<LoggerHelperOptions>();
        _output.WriteLine($"[Restituito] ApplicationName={options.ApplicationName}, Routes=[{string.Join(", ", options.Routes.Select(r => r.Sink))}]");

        Assert.Equal("LegacyApp", options.ApplicationName);
        Assert.Equal("Console", Assert.Single(options.Routes).Sink);
    }

    [Fact]
    public void AddLoggerHelper_ConfigurationAndFluent_AppendsRoutesAndOverridesApplicationName() {
        _output.WriteLine("[Scenario] JSON con ApplicationName \"JsonApp\" e route Console + fluent con WithApplicationName(\"FluentApp\") e AddRoute File/Error");
        _output.WriteLine("[Atteso] Il fluent ha la precedenza sul nome (\"FluentApp\") e le route si sommano: Console (JSON) poi File (fluent)");

        using var _ = new IsolatedEnvironment();
        var services = new ServiceCollection();

        services.AddLoggerHelper(InMemory(ConsoleRoute), b => b
            .WithApplicationName("FluentApp")
            .AddRoute("File", LogEventLevel.Error));

        using var provider = services.BuildServiceProvider();
        var options = provider.GetRequiredService<LoggerHelperOptions>();
        _output.WriteLine($"[Restituito] ApplicationName={options.ApplicationName}, Routes=[{string.Join(", ", options.Routes.Select(r => r.Sink))}]");

        Assert.Equal("FluentApp", options.ApplicationName);
        Assert.Equal(2, options.Routes.Count);
        Assert.Equal("Console", options.Routes[0].Sink);
        Assert.Equal("File", options.Routes[1].Sink);
    }

    [Fact]
    public void JsonWithoutRoutesPlusFluentRoute_DoesNotThrowAndMergesRoute() {
        _output.WriteLine("[Scenario] JSON senza route (solo ApplicationName) + fluent con AddRoute(\"Console\", Information)");
        _output.WriteLine("[Atteso] Nessuna eccezione \"No routes configured\": la route fluent basta e risulta l'unica");

        using var _ = new IsolatedEnvironment();
        var services = new ServiceCollection();

        services.AddLoggerHelper(InMemory(("LoggerHelper:ApplicationName", "NoJsonRoutes")),
            b => b.AddRoute("Console", LogEventLevel.Information));

        using var provider = services.BuildServiceProvider();
        var options = provider.GetRequiredService<LoggerHelperOptions>();
        _output.WriteLine($"[Restituito] Routes=[{string.Join(", ", options.Routes.Select(r => r.Sink))}]");

        Assert.Equal("Console", Assert.Single(options.Routes).Sink);
    }

    [Fact]
    public void JsonRoutesPlusFluentRoute_RoutesAreAdditive() {
        _output.WriteLine("[Scenario] JSON con route Console + fluent con AddRoute(\"File\", Error)");
        _output.WriteLine("[Atteso] Le route sono additive, in ordine: [Console, File]");

        using var _ = new IsolatedEnvironment();
        var services = new ServiceCollection();

        services.AddLoggerHelper(InMemory(ConsoleRoute), b => b.AddRoute("File", LogEventLevel.Error));

        using var provider = services.BuildServiceProvider();
        var routes = provider.GetRequiredService<LoggerHelperOptions>().Routes;
        _output.WriteLine($"[Restituito] Routes=[{string.Join(", ", routes.Select(r => r.Sink))}]");

        Assert.Equal(["Console", "File"], routes.Select(r => r.Sink).ToArray());
    }

    [Fact]
    public void JsonAndFluentWithoutRoutes_ThrowsNoRoutesConfigured() {
        _output.WriteLine("[Scenario] JSON senza route e fluent che imposta solo WithApplicationName(\"X\")");
        _output.WriteLine("[Atteso] InvalidOperationException \"No routes configured\": nessuna route da nessuna delle due fonti");

        using var _ = new IsolatedEnvironment();
        var services = new ServiceCollection();

        var ex = Assert.Throws<InvalidOperationException>(() =>
            services.AddLoggerHelper(InMemory(("LoggerHelper:ApplicationName", "None")), b => b.WithApplicationName("X")));
        _output.WriteLine($"[Restituito] {ex.GetType().Name}: {ex.Message}");

        Assert.Contains("No routes configured", ex.Message);
    }

    [Fact]
    public void JsonWithoutRoutesPlusFluentSinkConfig_SinkIsConfigured() {
        _output.WriteLine("[Scenario] JSON senza route + fluent con AddRoute(\"Seq\") e ConfigureSeq(ServerUrl=http://localhost:5341)");
        _output.WriteLine("[Atteso] Il sink Seq risulta caricato con Configured == true (la config fluent soddisfa il plugin)");

        using var _ = new IsolatedEnvironment();
        var services = new ServiceCollection();

        services.AddLoggerHelper(InMemory(("LoggerHelper:ApplicationName", "FluentSeq")), b => b
            .AddRoute("Seq", LogEventLevel.Information)
            .ConfigureSeq(o => o.ServerUrl = "http://localhost:5341"));

        using var provider = services.BuildServiceProvider();
        var seq = provider.GetRequiredService<ILoadedSinkStore>().GetAll().Single(s => s.SinkName == "Seq");
        _output.WriteLine($"[Restituito] Seq.Configured={seq.Configured}");

        Assert.True(seq.Configured);
    }

    [Fact]
    public void AddLoggerHelper_FluentWithoutApplicationName_KeepsJsonApplicationName() {
        _output.WriteLine("[Scenario] JSON con ApplicationName \"JsonApp\" + fluent che aggiunge solo una route File (senza WithApplicationName)");
        _output.WriteLine("[Atteso] ApplicationName resta quello del JSON: \"JsonApp\"");

        using var _ = new IsolatedEnvironment();
        var services = new ServiceCollection();

        services.AddLoggerHelper(InMemory(ConsoleRoute), b => b.AddRoute("File", LogEventLevel.Error));

        using var provider = services.BuildServiceProvider();

        _output.WriteLine($"[Restituito] ApplicationName={provider.GetRequiredService<LoggerHelperOptions>().ApplicationName}");
        Assert.Equal("JsonApp", provider.GetRequiredService<LoggerHelperOptions>().ApplicationName);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void AddLoggerHelper_ContextualLoggingFlag_RegistersBufferOnlyWhenEnabled(bool enabled) {
        _output.WriteLine($"[Scenario] JSON con LoggerHelper:General:EnableContextualLogging={enabled}");
        _output.WriteLine($"[Atteso] ContextualLogBuffer {(enabled ? "registrato" : "non registrato")} nel container DI (registrato solo se il flag è true)");

        using var _ = new IsolatedEnvironment();
        var services = new ServiceCollection();

        services.AddLoggerHelper(InMemory(ConsoleRoute
            .Append(("LoggerHelper:General:EnableContextualLogging", enabled.ToString())).ToArray()));

        using var provider = services.BuildServiceProvider();

        _output.WriteLine($"[Restituito] BufferRegistrato={provider.GetService<ContextualLogBuffer>() is not null}");
        Assert.Equal(enabled, provider.GetService<ContextualLogBuffer>() is not null);
    }

    [Fact]
    public void AddLoggerHelper_LoggingBuilderWithConfiguration_RegistersLoggerProvider() {
        _output.WriteLine("[Scenario] ILoggingBuilder.AddLoggerHelper(IConfiguration) con una route Console");
        _output.WriteLine("[Atteso] Tra i provider c'è un LoggerHelperProvider e ILoggerFactory crea logger per la categoria \"Cat\"");

        using var _ = new IsolatedEnvironment();
        var services = new ServiceCollection();

        services.AddLogging(lb => lb.AddLoggerHelper(InMemory(ConsoleRoute)));

        using var provider = services.BuildServiceProvider();

        _output.WriteLine($"[Restituito] Providers=[{string.Join(", ", provider.GetServices<ILoggerProvider>().Select(p => p.GetType().Name))}]");
        Assert.Contains(provider.GetServices<ILoggerProvider>(), p => p is LoggerHelperProvider);
        Assert.NotNull(provider.GetRequiredService<ILoggerFactory>().CreateLogger("Cat"));
    }

    [Fact]
    public void AddLoggerHelper_JsonFileInCurrentDirectory_ReplacesPassedConfiguration() {
        _output.WriteLine("[Scenario] File appsettings.LoggerHelper.json (ApplicationName \"FileApp\") nella cartella corrente e IConfiguration passata con ApplicationName \"JsonApp\"");
        _output.WriteLine("[Atteso] Il file nella cartella corrente sostituisce la configurazione passata: ApplicationName == \"FileApp\"");

        using var env = new IsolatedEnvironment();
        env.WriteFile("appsettings.LoggerHelper.json", JsonFileContent);
        var services = new ServiceCollection();

        services.AddLoggerHelper(InMemory(ConsoleRoute));

        using var provider = services.BuildServiceProvider();

        _output.WriteLine($"[Restituito] ApplicationName={provider.GetRequiredService<LoggerHelperOptions>().ApplicationName}");
        Assert.Equal("FileApp", provider.GetRequiredService<LoggerHelperOptions>().ApplicationName);
    }

    [Theory]
    [InlineData("ASPNETCORE_ENVIRONMENT")]
    [InlineData("DOTNET_ENVIRONMENT")]
    public void AddLoggerHelper_DevelopmentEnvironment_UsesDebugJsonFile(string envVar) {
        _output.WriteLine($"[Scenario] Variabile {envVar}=Development con appsettings.LoggerHelper.json (FileApp) e appsettings.LoggerHelper.debug.json (DebugApp)");
        _output.WriteLine("[Atteso] In Development viene usato il file .debug.json: ApplicationName == \"DebugApp\"");

        using var env = new IsolatedEnvironment();
        env.WriteFile("appsettings.LoggerHelper.json", JsonFileContent);
        env.WriteFile("appsettings.LoggerHelper.debug.json", JsonFileContent.Replace("FileApp", "DebugApp"));
        Environment.SetEnvironmentVariable(envVar, "Development");
        var services = new ServiceCollection();

        services.AddLoggerHelper(InMemory(ConsoleRoute));

        using var provider = services.BuildServiceProvider();

        _output.WriteLine($"[Restituito] ApplicationName={provider.GetRequiredService<LoggerHelperOptions>().ApplicationName}");
        Assert.Equal("DebugApp", provider.GetRequiredService<LoggerHelperOptions>().ApplicationName);
    }

    [Fact]
    public void AddLoggerHelper_JsonFileInCurrentDirectory_LoadsFileAndAllowsDeletion() {
        _output.WriteLine("[Scenario] File appsettings.LoggerHelper.json nella cartella corrente: dopo AddLoggerHelper lo sovrascrivo e lo cancello");
        _output.WriteLine("[Atteso] Il file non resta aperto dal loader (nessuna IOException) e dopo Delete non esiste più");

        using var env = new IsolatedEnvironment();
        env.WriteFile("appsettings.LoggerHelper.json", JsonFileContent);
        var path = Path.Combine(Directory.GetCurrentDirectory(), "appsettings.LoggerHelper.json");
        var services = new ServiceCollection();

        services.AddLoggerHelper(InMemory(ConsoleRoute));

        // Would throw IOException if the file stayed open
        File.WriteAllText(path, "{}");
        File.Delete(path);
        _output.WriteLine($"[Restituito] FileEsiste={File.Exists(path)}");
        Assert.False(File.Exists(path));
    }

    [Fact]
    public void JsonPlusFluentFlags_FluentFlagsApplied() {
        _output.WriteLine("[Scenario] JSON con route Console (General di default) + fluent EnableRequestResponseLogging().EnableRenderedMessage().DisableOpenTelemetry()");
        _output.WriteLine("[Atteso] General: RequestResponseLogging=true, RenderedMessage=true, OpenTelemetry=false");

        using var _ = new IsolatedEnvironment();
        var services = new ServiceCollection();

        services.AddLoggerHelper(InMemory(ConsoleRoute), b => b
            .EnableRequestResponseLogging().EnableRenderedMessage().DisableOpenTelemetry());

        using var provider = services.BuildServiceProvider();
        var g = provider.GetRequiredService<LoggerHelperOptions>().General;
        _output.WriteLine($"[Restituito] RequestResponse={g.EnableRequestResponseLogging}, Rendered={g.EnableRenderedMessage}, OTel={g.EnableOpenTelemetry}");

        Assert.True(g.EnableRequestResponseLogging);
        Assert.True(g.EnableRenderedMessage);
        Assert.False(g.EnableOpenTelemetry);
    }

    [Fact]
    public void JsonFlagsTrue_FluentNotCalled_JsonFlagsKept() {
        _output.WriteLine("[Scenario] JSON con EnableRequestResponseLogging=true ed EnableRenderedMessage=true, fluent che aggiunge solo una route");
        _output.WriteLine("[Atteso] I flag del JSON restano true e OpenTelemetry resta true (default)");

        using var _ = new IsolatedEnvironment();
        var services = new ServiceCollection();

        services.AddLoggerHelper(InMemory(ConsoleRoute
            .Append(("LoggerHelper:General:EnableRequestResponseLogging", "true"))
            .Append(("LoggerHelper:General:EnableRenderedMessage", "true")).ToArray()),
            b => b.AddRoute("File", LogEventLevel.Error));

        using var provider = services.BuildServiceProvider();
        var g = provider.GetRequiredService<LoggerHelperOptions>().General;
        _output.WriteLine($"[Restituito] RequestResponse={g.EnableRequestResponseLogging}, Rendered={g.EnableRenderedMessage}, OTel={g.EnableOpenTelemetry}");

        Assert.True(g.EnableRequestResponseLogging);
        Assert.True(g.EnableRenderedMessage);
        Assert.True(g.EnableOpenTelemetry);
    }

    [Fact]
    public void JsonOtelDisabled_FluentDefault_OtelStaysDisabled() {
        _output.WriteLine("[Scenario] JSON con EnableOpenTelemetry=false, fluent senza chiamare DisableOpenTelemetry");
        _output.WriteLine("[Atteso] OpenTelemetry resta false (il fluent non puo' riaccenderlo)");

        using var _ = new IsolatedEnvironment();
        var services = new ServiceCollection();

        services.AddLoggerHelper(InMemory(ConsoleRoute
            .Append(("LoggerHelper:General:EnableOpenTelemetry", "false")).ToArray()),
            b => b.AddRoute("File", LogEventLevel.Error));

        using var provider = services.BuildServiceProvider();
        var otel = provider.GetRequiredService<LoggerHelperOptions>().General.EnableOpenTelemetry;
        _output.WriteLine($"[Restituito] OTel={otel}");

        Assert.False(otel);
    }

    [Fact]
    public void JsonMaskingPlusFluentMasking_RulesUnioned() {
        _output.WriteLine("[Scenario] JSON con masking (preset Email, property \"Password\") + fluent masking (preset JwtToken, property \"password\", MaskText \"[X]\")");
        _output.WriteLine("[Atteso] Preset Email+JwtToken, una sola SensitiveProperties (case-insensitive), MaskText \"[X]\", Enabled=true");

        using var _ = new IsolatedEnvironment();
        var services = new ServiceCollection();

        services.AddLoggerHelper(InMemory(ConsoleRoute
            .Append(("LoggerHelper:SensitiveDataMasking:Enabled", "true"))
            .Append(("LoggerHelper:SensitiveDataMasking:Presets:0", "Email"))
            .Append(("LoggerHelper:SensitiveDataMasking:SensitiveProperties:0", "Password")).ToArray()),
            b => b.EnableSensitiveDataMasking(m => {
                m.Presets.Add("JwtToken");
                m.SensitiveProperties.Add("password");
                m.MaskText = "[X]";
            }));

        using var provider = services.BuildServiceProvider();
        var m = provider.GetRequiredService<LoggerHelperOptions>().SensitiveDataMasking;
        _output.WriteLine($"[Restituito] Enabled={m.Enabled}, Presets=[{string.Join(",", m.Presets)}], Props=[{string.Join(",", m.SensitiveProperties)}], MaskText={m.MaskText}");

        Assert.True(m.Enabled);
        Assert.Equal(["Email", "JwtToken"], m.Presets);
        Assert.Single(m.SensitiveProperties);
        Assert.Equal("[X]", m.MaskText);
    }

    [Fact]
    public void JsonMaskingEnabled_FluentNotCalled_JsonMaskingKept() {
        _output.WriteLine("[Scenario] JSON con masking abilitato (preset Email, MaskText \"[J]\"), fluent che aggiunge solo una route (masking fluent disabilitato)");
        _output.WriteLine("[Atteso] Il masking del JSON resta invariato: Enabled=true, preset Email, MaskText \"[J]\"");

        using var _ = new IsolatedEnvironment();
        var services = new ServiceCollection();

        services.AddLoggerHelper(InMemory(ConsoleRoute
            .Append(("LoggerHelper:SensitiveDataMasking:Enabled", "true"))
            .Append(("LoggerHelper:SensitiveDataMasking:MaskText", "[J]"))
            .Append(("LoggerHelper:SensitiveDataMasking:Presets:0", "Email")).ToArray()),
            b => b.AddRoute("File", LogEventLevel.Error));

        using var provider = services.BuildServiceProvider();
        var m = provider.GetRequiredService<LoggerHelperOptions>().SensitiveDataMasking;
        _output.WriteLine($"[Restituito] Enabled={m.Enabled}, Presets=[{string.Join(",", m.Presets)}], MaskText={m.MaskText}");

        Assert.True(m.Enabled);
        Assert.Equal(["Email"], m.Presets);
        Assert.Equal("[J]", m.MaskText);
    }

    [Theory]
    [InlineData("false", true, true)]
    [InlineData("true", false, true)]
    [InlineData("false", false, false)]
    public void SelfLogging_JsonAndFluent_AreOred(string json, bool fluent, bool expected) {
        _output.WriteLine($"[Scenario] JSON EnableSelfLogging={json}, fluent EnableSelfLogging()={fluent}");
        _output.WriteLine($"[Atteso] EnableSelfLogging={expected}");

        using var _ = new IsolatedEnvironment();
        var services = new ServiceCollection();

        services.AddLoggerHelper(InMemory(ConsoleRoute
            .Append(("LoggerHelper:General:EnableSelfLogging", json)).ToArray()),
            b => { if (fluent) b.EnableSelfLogging(); else b.AddRoute("File", LogEventLevel.Error); });

        using var provider = services.BuildServiceProvider();
        var actual = provider.GetRequiredService<LoggerHelperOptions>().General.EnableSelfLogging;
        _output.WriteLine($"[Restituito] EnableSelfLogging={actual}");

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void JsonMaskingRules_FluentMaskingDisabled_JsonRulesUntouched() {
        _output.WriteLine("[Scenario] JSON con masking abilitato e una regola, fluent senza EnableSensitiveDataMasking");
        _output.WriteLine("[Atteso] Enabled=true, una sola regola (quella del JSON)");

        using var _ = new IsolatedEnvironment();
        var services = new ServiceCollection();

        services.AddLoggerHelper(InMemory(ConsoleRoute
            .Append(("LoggerHelper:SensitiveDataMasking:Enabled", "true"))
            .Append(("LoggerHelper:SensitiveDataMasking:Rules:0:Name", "Card"))
            .Append(("LoggerHelper:SensitiveDataMasking:Rules:0:Pattern", @"\d{16}")).ToArray()),
            b => b.AddRoute("File", LogEventLevel.Error));

        using var provider = services.BuildServiceProvider();
        var m = provider.GetRequiredService<LoggerHelperOptions>().SensitiveDataMasking;
        _output.WriteLine($"[Restituito] Enabled={m.Enabled}, Rules=[{string.Join(",", m.Rules.Select(r => r.Pattern))}]");

        Assert.True(m.Enabled);
        Assert.Equal(@"\d{16}", Assert.Single(m.Rules).Pattern);
    }

    [Fact]
    public void JsonAndFluentSameRule_RuleNotDuplicated() {
        _output.WriteLine("[Scenario] Stessa MaskingRule (Pattern \\d{16}) in JSON e nel fluent, piu' una seconda copia nel fluent");
        _output.WriteLine("[Atteso] Una sola regola, quella del JSON (Name \"Card\")");

        using var _ = new IsolatedEnvironment();
        var services = new ServiceCollection();

        services.AddLoggerHelper(InMemory(ConsoleRoute
            .Append(("LoggerHelper:SensitiveDataMasking:Enabled", "true"))
            .Append(("LoggerHelper:SensitiveDataMasking:Rules:0:Name", "Card"))
            .Append(("LoggerHelper:SensitiveDataMasking:Rules:0:Pattern", @"\d{16}")).ToArray()),
            b => b.EnableSensitiveDataMasking(m => {
                m.Rules.Add(new MaskingRule { Name = "CardFluent", Pattern = @"\d{16}" });
                m.Rules.Add(new MaskingRule { Name = "CardFluent2", Pattern = @"\d{16}" });
            }));

        using var provider = services.BuildServiceProvider();
        var m = provider.GetRequiredService<LoggerHelperOptions>().SensitiveDataMasking;
        _output.WriteLine($"[Restituito] Rules=[{string.Join(",", m.Rules.Select(r => r.Name))}]");

        Assert.Equal("Card", Assert.Single(m.Rules).Name);
    }

    [Fact]
    public void JsonAndFluentDifferentRules_BothKept() {
        _output.WriteLine("[Scenario] Regola JSON e regola fluent con Pattern diversi");
        _output.WriteLine("[Atteso] Due regole, nell'ordine JSON poi fluent");

        using var _ = new IsolatedEnvironment();
        var services = new ServiceCollection();

        services.AddLoggerHelper(InMemory(ConsoleRoute
            .Append(("LoggerHelper:SensitiveDataMasking:Enabled", "true"))
            .Append(("LoggerHelper:SensitiveDataMasking:Rules:0:Name", "Card"))
            .Append(("LoggerHelper:SensitiveDataMasking:Rules:0:Pattern", @"\d{16}")).ToArray()),
            b => b.EnableSensitiveDataMasking(m =>
                m.Rules.Add(new MaskingRule { Name = "Ssn", Pattern = @"\d{3}-\d{2}-\d{4}" })));

        using var provider = services.BuildServiceProvider();
        var m = provider.GetRequiredService<LoggerHelperOptions>().SensitiveDataMasking;
        _output.WriteLine($"[Restituito] Rules=[{string.Join(",", m.Rules.Select(r => r.Name))}]");

        Assert.Equal(["Card", "Ssn"], m.Rules.Select(r => r.Name));
    }

    private sealed class IsolatedEnvironment : IDisposable {
        private readonly string _originalCwd = Directory.GetCurrentDirectory();
        private readonly string? _aspnetEnv = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");
        private readonly string? _dotnetEnv = Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT");
        private readonly string _dir = Path.Combine(Path.GetTempPath(), "lh-a25-" + Guid.NewGuid());

        public IsolatedEnvironment() {
            Directory.CreateDirectory(_dir);
            Directory.SetCurrentDirectory(_dir);
            Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", null);
            Environment.SetEnvironmentVariable("DOTNET_ENVIRONMENT", null);
        }

        public void WriteFile(string name, string json) => File.WriteAllText(Path.Combine(_dir, name), json);

        public void Dispose() {
            Directory.SetCurrentDirectory(_originalCwd);
            Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", _aspnetEnv);
            Environment.SetEnvironmentVariable("DOTNET_ENVIRONMENT", _dotnetEnv);
            try { Directory.Delete(_dir, recursive: true); } catch (IOException) { } catch (UnauthorizedAccessException) { }
        }
    }
}
