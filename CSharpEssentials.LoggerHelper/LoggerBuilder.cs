using CSharpEssentials.LoggerHelper.CustomSinks;
using Microsoft.Extensions.Configuration;
using Microsoft.VisualBasic;
using Serilog;
using System.Diagnostics;
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
    internal LoggerBuilder AddDynamicSinks() {
        var baseDir = AppContext.BaseDirectory;
        path = $"AddDynamicSinks Path: {baseDir}";
        IEnumerable<string> pluginDlls = Enumerable.Empty<string>();
        try {
            if (Directory.Exists(baseDir)) {
                pluginDlls = Directory.EnumerateFiles(baseDir, "CSharpEssentials.LoggerHelper.Sink.*.dll");
            }
        } catch (Exception ex) {
            _initializationErrors.Add(("Impossibile enumerare i plugin sinks", ex));
        }

        foreach (var dllPath in pluginDlls) {
            try {
                var ctx = new TolerantPluginLoadContext(dllPath);
                var asmName = new AssemblyName(Path.GetFileNameWithoutExtension(dllPath));
                ctx.LoadFromAssemblyName(asmName);
            } catch {}
        }

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

        foreach (var condition in _serilogConfig.SerilogCondition ?? Enumerable.Empty<SerilogCondition>()) {
            if (condition.Level == null || condition.Level.Count == 0)
                continue;

            var plugin = SinkPluginRegistry.All
                .FirstOrDefault(p => p.CanHandle(condition.Sink));

            if (plugin != null) {
                if (_excludeSinkFile
                    && condition.Sink.Equals("File", StringComparison.OrdinalIgnoreCase)) {
                    continue;
                }

                plugin.HandleSink(_config, condition, _serilogConfig);
            } else {
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
