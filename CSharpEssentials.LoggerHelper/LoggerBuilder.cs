using CSharpEssentials.LoggerHelper.CustomSinks;
using Microsoft.Extensions.Configuration;
using Serilog;
using System.Runtime.Loader;

namespace CSharpEssentials.LoggerHelper;
/// <summary>
/// Responsible for building the Serilog logger configuration dynamically based on the provided appsettings.json configuration.
/// </summary>
internal class LoggerBuilder {
    private readonly LoggerConfiguration _config;
    private readonly SerilogConfiguration _serilogConfig;
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
        var configuration = new ConfigurationBuilder()
#if DEBUG
    .AddJsonFile("appsettings.LoggerHelper.debug.json")
#else
    .AddJsonFile("appsettings.LoggerHelper.json")
#endif
        .Build();
        _serilogConfig = configuration.GetSection("Serilog:SerilogConfiguration").Get<SerilogConfiguration>();

        var appName = _serilogConfig.ApplicationName;
        _config = new LoggerConfiguration().ReadFrom.Configuration(configuration)
            .WriteTo.Sink(new OpenTelemetryLogEventSink())//TODO: da configurare
            .Enrich.WithProperty("ApplicationName", appName)
            .Enrich.With<RenderedMessageEnricher>();
        var selfLogPath = Path.Combine(_serilogConfig?.SerilogOption?.File?.Path, "serilog-selflog.txt");
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
    }
    /// <summary>
    /// Dynamically adds sinks to the LoggerConfiguration based on conditions specified in the Serilog configuration.
    /// </summary>
    /// <returns>The current instance of LoggerBuilder for chaining.</returns>
    internal LoggerBuilder AddDynamicSinks() {
        var baseDir = AppContext.BaseDirectory;
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
                // delego completamente al plugin
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
                    case "Console":
                        _config.WriteTo.Conditional(
                            evt => _serilogConfig.IsSinkLevelMatch(condition.Sink, evt.Level),
                            wt => wt.Console()
                        );
                        break;
                }
            }
        }

        return this;
    }
}
