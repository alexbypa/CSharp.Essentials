using CSharpEssentials.LoggerHelper.CustomSinks;
using Microsoft.Extensions.Configuration;
using Microsoft.VisualBasic;
using Serilog;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.Loader;

namespace CSharpEssentials.LoggerHelper;
/// <summary>
/// Responsible for building the Serilog logger configuration dynamically based on the provided appsettings.json configuration.
/// </summary>
internal class LoggerBuilder {
    private readonly LoggerConfiguration _config;
    private readonly SerilogConfiguration _serilogConfig;
    /// <summary>
    /// Dynamically loads and registers available sink plugins by scanning the current
    /// application's base directory for assemblies matching the sink naming convention.
    /// 
    /// If the "_excludeSinkFile" flag is set to true (e.g., due to missing log file directory),
    /// the "File" sink plugin will be excluded from registration.
    /// </summary>
    internal static IConfiguration BuildLoggerConfiguration() {
        var builder = new ConfigurationBuilder();

        // Carica sempre l'appsettings di default
        builder.AddJsonFile("appsettings.LoggerHelper.json", optional: true, reloadOnChange: true);

        // Leggi la variabile di ambiente
        var envName = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT")
                       ?? Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT");

        // Se l'ambiente è "Development", carica anche il file di Debug
        if (string.Equals(envName, "Development", StringComparison.OrdinalIgnoreCase)) {
            builder.AddJsonFile("appsettings.LoggerHelper.debug.json", optional: true, reloadOnChange: true);
        }

        return builder.Build();
    }

    /// <summary>
    /// Builds and returns the configured Serilog logger instance.
    /// </summary>
    /// <returns>The created <see cref="ILogger"/> instance.</returns>
    public ILogger Build() => _config.CreateLogger();
    /// <summary>
    /// Initializes a new instance of the <see cref="LoggerBuilder"/> class.
    /// Reads the Serilog configuration section and sets up basic enrichers and self-logging.
    /// </summary>
    /// <param name="configuration">Application configuration (e.g., appsettings.json).</param>
    internal LoggerBuilder() {
        var configuration = BuildLoggerConfiguration();
        _serilogConfig = configuration.GetSection("Serilog:SerilogConfiguration").Get<SerilogConfiguration>();

        var appName = _serilogConfig.ApplicationName;
        _config = new LoggerConfiguration().ReadFrom.Configuration(configuration)
            .WriteTo.Sink(new OpenTelemetryLogEventSink())//TODO: da configurare
            .Enrich.WithProperty("ApplicationName", appName)
            .Enrich.With<RenderedMessageEnricher>();
        var selfLogPath = Path.Combine(_serilogConfig?.SerilogOption?.File?.Path, "serilog-selflog.txt");

        var logFileDir = Path.GetDirectoryName(selfLogPath);
        try {
            if (!string.IsNullOrEmpty(logFileDir) && !Directory.Exists(logFileDir)) {
                Directory.CreateDirectory(logFileDir);
            }
            var stream = new FileStream(
                selfLogPath,
                FileMode.Append,
                FileAccess.Write,
                FileShare.ReadWrite
            );
            var writer = new StreamWriter(stream) { AutoFlush = true };
            Serilog.Debugging.SelfLog.Enable(msg => {
                writer.WriteLine(msg);
            });
        } catch (Exception ex) {
            _initializationErrors.Add(($"Could not create log directory '{logFileDir}'", ex));
            _excludeSinkFile = true; // 🚀 Attivo il flag
        }
    }
    public delegate void ErrorHandler(string message, Exception exception);
    private readonly List<(string Message, Exception Exception)> _initializationErrors = new();
    public IEnumerable<(string Message, Exception Exception)> GetInitializationErrors() => _initializationErrors;
    private bool _excludeSinkFile;

    /// <summary>
    /// Dynamically adds sinks to the LoggerConfiguration based on conditions specified in the Serilog configuration.
    /// </summary>
    /// <returns>The current instance of LoggerBuilder for chaining.</returns>
    internal LoggerBuilder AddDynamicSinks(out string path) {
        var baseDir = AppContext.BaseDirectory;
        path = $"AddDynamicSinks Path: {baseDir}";

        // 1) Individuo tutti i file DLL che corrispondono al pattern "CSharpEssentials.LoggerHelper.Sink.*.dll"
        IEnumerable<string> pluginDlls = Enumerable.Empty<string>();
        try {
            if (Directory.Exists(baseDir)) {
                pluginDlls = Directory.EnumerateFiles(baseDir, "CSharpEssentials.LoggerHelper.Sink.*.dll");
            }
        } catch (Exception ex) {
            _initializationErrors.Add(("Impossibile enumerare i plugin sinks", ex));
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
                    _initializationErrors.Add(($"Errore registrando il plugin {t.FullName}", ex));
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
            } else {
                // Se non esiste un ISinkPlugin per questo Sink, gestisco manualmente Telegram/Email
                switch (condition.Sink) {
                    case "Telegram":
                        _config.WriteTo.Conditional(
                            evt => _serilogConfig.IsSinkLevelMatch(condition.Sink, evt.Level),
                            wt => wt.Sink(new CustomTelegramSink(
                                _serilogConfig?.SerilogOption?.TelegramOption?.Api_Key,
                                _serilogConfig?.SerilogOption?.TelegramOption?.chatId,
                                new CustomTelegramSinkFormatter()))
                        );
                        break;

                    case "Email":
                        _config.WriteTo.Conditional(
                            evt => _serilogConfig.IsSinkLevelMatch(condition.Sink, evt.Level),
                            wt => wt.Sink(new CustomEmailSink(
                                smtpServer: _serilogConfig.SerilogOption?.Email.Host,
                                smtpPort: (int)_serilogConfig.SerilogOption?.Email.Port,
                                fromEmail: _serilogConfig.SerilogOption?.Email.From,
                                toEmail: string.Join(",", _serilogConfig.SerilogOption?.Email.To),
                                username: _serilogConfig.SerilogOption?.Email.username,
                                password: _serilogConfig.SerilogOption?.Email.password,
                                subjectPrefix: "[LoggerHelper]",
                                enableSsl: (bool)_serilogConfig.SerilogOption?.Email.EnableSsl,
                                templatePath: _serilogConfig.SerilogOption?.Email.TemplatePath
                            ))
                        );
                        break;

                        // Aggiungi qui altri casi manuali se ti servono
                }
            }
        }

        return this;
    }

    internal LoggerBuilder AddDynamicSinks_deprecate(out string path) {
        var baseDir = AppContext.BaseDirectory;

        path = "AddDynamicSinks Path: " + baseDir;
        foreach (var dll in Directory.EnumerateFiles(baseDir, "CSharpEssentials.LoggerHelper.Sink.*.dll")) {
            try {
                AssemblyLoadContext.Default.LoadFromAssemblyPath(dll);
            } catch {
                // ignora i DLL inutili o non validi
            }
        }
        var assemblies = AssemblyLoadContext.Default.Assemblies;

        if (!SinkPluginRegistry.All.Any()) {
            var pluginTypes = assemblies
                .SelectMany(a => {
                    try { return a.GetTypes(); } catch { return Array.Empty<Type>(); }
                })
                .Where(t =>
                    typeof(ISinkPlugin).IsAssignableFrom(t)
                    && !t.IsInterface
                    && !t.IsAbstract);

            foreach (var t in pluginTypes) {
                var instance = (ISinkPlugin)Activator.CreateInstance(t)!;
                SinkPluginRegistry.Register(instance);
            }
        }

        foreach (var condition in _serilogConfig.SerilogCondition ?? Enumerable.Empty<SerilogCondition>()) {
            if (condition.Level == null || !condition.Level.Any())
                continue;

            var plugin = SinkPluginRegistry.All
                .FirstOrDefault(p => p.CanHandle(condition.Sink));
            if (plugin != null) {
                if (!(_excludeSinkFile && condition.Sink.Contains("File", StringComparison.InvariantCultureIgnoreCase)))
                    plugin.HandleSink(_config, condition, _serilogConfig);
            } else {
                switch (condition.Sink) {
                    case "Telegram":
                        _config.WriteTo.Conditional(
                            evt => _serilogConfig.IsSinkLevelMatch(condition.Sink, evt.Level),
                            wt => {
                                wt.Sink(new CustomTelegramSink(
                                    _serilogConfig?.SerilogOption?.TelegramOption?.Api_Key,
                                    _serilogConfig?.SerilogOption?.TelegramOption?.chatId,
                                    new CustomTelegramSinkFormatter()));
                            });
                        break;
                    case "Email":
                        _config.WriteTo.Conditional(
                            evt => _serilogConfig.IsSinkLevelMatch(condition.Sink, evt.Level),
                            wt => wt.Sink(new CustomEmailSink(
                                smtpServer: _serilogConfig.SerilogOption?.Email.Host,
                                smtpPort: (int)_serilogConfig.SerilogOption?.Email.Port,
                                fromEmail: _serilogConfig.SerilogOption?.Email.From,
                                toEmail: string.Join(",", _serilogConfig.SerilogOption?.Email.To),
                                username: _serilogConfig.SerilogOption?.Email.username,
                                password: _serilogConfig.SerilogOption?.Email.password,
                                subjectPrefix: "[LoggerHelper]",
                                enableSsl: (bool)_serilogConfig.SerilogOption?.Email?.EnableSsl,
                                templatePath: _serilogConfig.SerilogOption?.Email?.TemplatePath
                            ))
                        );
                        break;
                }
            }
        }

        return this;
    }
}
