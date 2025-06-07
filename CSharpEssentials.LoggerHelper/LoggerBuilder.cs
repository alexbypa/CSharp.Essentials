using CSharpEssentials.LoggerHelper.model;
using Microsoft.Extensions.Configuration;
using Serilog;
using System.Collections.Concurrent;
using System.Reflection;
using System.Runtime.Loader;

namespace CSharpEssentials.LoggerHelper;
/// <summary>
/// Responsible for building the Serilog logger configuration dynamically based on the provided appsettings.json configuration.
/// </summary>
internal class LoggerBuilder {
    private readonly LoggerConfiguration _config;
    private readonly SerilogConfiguration _serilogConfig;
    private static string fileNameSettings = "";
    /// <summary>
    /// Dynamically loads and registers available sink plugins by scanning the current
    /// application's base directory for assemblies matching the sink naming convention.
    /// 
    /// If the "_excludeSinkFile" flag is set to true (e.g., due to missing log file directory),
    /// the "File" sink plugin will be excluded from registration.
    /// </summary>
    internal static IConfiguration BuildLoggerConfiguration() {
        var builder = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory());

        // Leggi la variabile di ambiente
        var envName = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT")
                       ?? Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT");

        fileNameSettings = envName?.Equals("Development", StringComparison.OrdinalIgnoreCase)
                   == true
                   ? "appsettings.LoggerHelper.debug.json"
                   : "appsettings.LoggerHelper.json";

        if (string.Equals(envName, "Development", StringComparison.OrdinalIgnoreCase)) {
            builder.AddJsonFile("appsettings.LoggerHelper.debug.json", optional: false, reloadOnChange: true);
        }
        try {
            return builder.Build();
        } catch (FileNotFoundException fnf) {
            throw new InvalidOperationException($"Configuration File '{fileNameSettings}' not found", fnf);
        }
    }

    /// <summary>
    /// Builds and returns the configured Serilog logger instance.
    /// </summary>
    /// <returns>The created <see cref="ILogger"/> instance.</returns>
    public ILogger Build() => _config.CreateLogger();

    internal ConcurrentQueue<LogErrorEntry> _initializationErrors = new ConcurrentQueue<LogErrorEntry>();
    /// <summary>
    /// Initializes a new instance of the <see cref="LoggerBuilder"/> class.
    /// Reads the Serilog configuration section and sets up basic enrichers and self-logging.
    /// </summary>
    /// <param name="configuration">Application configuration (e.g., appsettings.json).</param>
    /// <exception cref="InvalidOperationException">
    /// Thrown if the `Serilog:SerilogConfiguration` section is missing.  
    /// See <see href="https://github.com/alexbypa/CSharp.Essentials/blob/TestLogger/LoggerHelperDemo/LoggerHelperDemo/Readme.md#installation">Configuration docs</see> for more info.
    /// </exception>
    internal LoggerBuilder() {
    var configuration = BuildLoggerConfiguration();
        _serilogConfig = configuration.GetSection("Serilog:SerilogConfiguration").Get<SerilogConfiguration>();
        if (_serilogConfig == null)
            throw new InvalidOperationException($"Section 'Serilog:SerilogConfiguration' not found on {fileNameSettings}. See Documentation https://github.com/alexbypa/CSharp.Essentials/blob/TestLogger/LoggerHelperDemo/LoggerHelperDemo/Readme.md#installation");
        if (_serilogConfig.SerilogOption == null)
            throw new InvalidOperationException($"Section 'Serilog:SerilogConfiguration:SerilogOption' not found on {fileNameSettings}. See Documentation https://github.com/alexbypa/CSharp.Essentials/blob/TestLogger/LoggerHelperDemo/LoggerHelperDemo/Readme.md#installation");
        if (_serilogConfig.SerilogCondition == null)
            throw new InvalidOperationException($"Section 'Serilog:SerilogConfiguration:SerilogCondition' not found on {fileNameSettings}. See Documentation https://github.com/alexbypa/CSharp.Essentials/blob/TestLogger/LoggerHelperDemo/LoggerHelperDemo/Readme.md#installation");

        var appName = _serilogConfig.ApplicationName;
        _config = new LoggerConfiguration().ReadFrom.Configuration(configuration)
            .WriteTo.Sink(new OpenTelemetryLogEventSink())//TODO: da configurare
            .Enrich.WithProperty("ApplicationName", appName)
            .Enrich.With<RenderedMessageEnricher>();
    }
    private bool _excludeSinkFile;
    /// <summary>
    /// Dynamically adds sinks to the LoggerConfiguration based on conditions specified in the Serilog configuration.
    /// </summary>
    /// <returns>The current instance of LoggerBuilder for chaining.</returns>
    internal LoggerBuilder AddDynamicSinks(out string path, out string SinkNameInError, ref ConcurrentQueue<LogErrorEntry> _Errors) {
        SinkNameInError = "";
        var baseDir = AppContext.BaseDirectory;
        path = $"AddDynamicSinks Path: {baseDir}";

        // 1) Individuo tutti i file DLL che corrispondono al pattern "CSharpEssentials.LoggerHelper.Sink.*.dll"
        IEnumerable<string> pluginDlls = Enumerable.Empty<string>();
        try {
            if (Directory.Exists(baseDir)) {
                pluginDlls = Directory.EnumerateFiles(baseDir, "CSharpEssentials.LoggerHelper.Sink.*.dll");
            }
        } catch (Exception ex) {
            _initializationErrors.Enqueue(
                new LogErrorEntry {
                    Timestamp = DateTime.UtcNow,
                    SinkName = "Config",
                    ErrorMessage = $"Plauing Sinks not found : {ex.Message}",
                    ContextInfo = AppContext.BaseDirectory
                });
        }

        // 2) Per ciascun DLL, provo a caricarlo in un contesto “tollerante”
        //    che ignora le eccezioni di dipendenza non trovata.
        foreach (var dllPath in pluginDlls) {
            try {
                // Creo un nuovo AssemblyLoadContext che non “crolla” se mancano dipendenze
                var ctx = new TolerantPluginLoadContext(dllPath);
                // Chiedo di caricare l’assembly a partire dal suo AssemblyName
                // (il metodo LoadFromAssemblyName farà partire la risoluzione tramite _resolver interno)
                var asmName = new AssemblyName(Path.GetFileNameWithoutExtension(dllPath));
                ctx.LoadFromAssemblyName(asmName);
                // Se dovessero mancare dipendenze (Es. Serilog.Formatting.Elasticsearch),
                // il TolerantPluginLoadContext cattura l’errore e torna null senza sollevare.
            } catch {
                // Se il DLL non è nemmeno un .NET assembly valido, lo ignoriamo del tutto.
            }
        }

        // 3) Se non ho registrato ancora alcun plugin, estraggo da tutti gli assembly caricati
        //    le classi che implementano ISinkPlugin e le registro.
        if (!SinkPluginRegistry.All.Any()) {
            var assemblies = AssemblyLoadContext.Default.Assemblies;
            var pluginTypes = assemblies
                .SelectMany(a => {
                    try { return a.GetTypes(); } catch { return Array.Empty<Type>(); }
                })
                .Where(t =>
                    typeof(ISinkPlugin).IsAssignableFrom(t)
                    && !t.IsInterface
                    && !t.IsAbstract
                );

            foreach (var t in pluginTypes) {
                try {
                    var instance = (ISinkPlugin)Activator.CreateInstance(t)!;
                    SinkPluginRegistry.Register(instance);
                } catch (Exception ex) {
                    _Errors.Enqueue(
                        new LogErrorEntry {
                            Timestamp = DateTime.UtcNow,
                            SinkName = t.Name,
                            ErrorMessage = ex.Message,
                            ContextInfo = AppContext.BaseDirectory
                        });
                    SinkNameInError = t.Name;
                }
            }
        }

        // 4) Infine, itero le condizioni e invoco HandleSink soltanto se esiste il plugin
        foreach (var condition in _serilogConfig.SerilogCondition ?? Enumerable.Empty<SerilogCondition>()) {
            // Se non è stato impostato un livello per questo sink => skip
            if (condition.Level == null || condition.Level.Count == 0)
                continue;

            // Trovo un plugin in grado di gestire questo “Sink”
            var plugin = SinkPluginRegistry.All
                .FirstOrDefault(p => p.CanHandle(condition.Sink));

            if (plugin != null) {
                // Se si tratta di File ma ho forzato l’esclusione, skippo
                if (_excludeSinkFile
                    && condition.Sink.Equals("File", StringComparison.OrdinalIgnoreCase)) {
                    continue;
                }

                plugin.HandleSink(_config, condition, _serilogConfig);
            }
        }
        return this;
    }
}
