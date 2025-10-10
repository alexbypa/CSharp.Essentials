using CSharpEssentials.LoggerHelper.Configuration;
using CSharpEssentials.LoggerHelper.model;
using Serilog;
using Serilog.Debugging;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.Loader;

namespace CSharpEssentials.LoggerHelper;
/// <summary>
/// Responsible for building the Serilog logger configuration dynamically based on the provided appsettings.json configuration.
/// </summary>
internal class LoggerBuilder {
    private LoggerConfiguration _config;
    private SerilogConfiguration _serilogConfig;
   internal LoggerBuilder() {
        _config = LoggerConfigHelper.LoggerConfig;
        _serilogConfig = LoggerConfigHelper.SerilogConfig;
    }
    /// <summary>
    /// Builds and returns the configured Serilog logger instance.
    /// </summary>
    /// <returns>The created <see cref="ILogger"/> instance.</returns>
    public ILogger Build() => _config.CreateLogger();

    internal ConcurrentQueue<LogErrorEntry> _initializationErrors = new ConcurrentQueue<LogErrorEntry>();
    
    private bool _excludeSinkFile;

    /// <summary>
    /// Dynamically adds sinks to the LoggerConfiguration based on conditions specified in the Serilog configuration.
    /// </summary>
    /// <returns>The current instance of LoggerBuilder for chaining.</returns>
    internal LoggerBuilder AddDynamicSinks(out string path, out string SinkNameInError, ref List<LogErrorEntry> _Errors, ref List<LoadedSinkInfo> SinksLoaded) {
        SinkNameInError = "";
        var baseDir = AppContext.BaseDirectory;
        path = $"AddDynamicSinks Path: {baseDir}";
        Console.WriteLine($"[dbg AddDynamicSinks] path:{path}");
        var pluginDlls = Directory
          .EnumerateFiles(baseDir, "CSharpEssentials.LoggerHelper.Sink.*.dll");

        Console.WriteLine($"[dbg AddDynamicSinks] Found {string.Join(", ", pluginDlls.ToArray())} potential plugins");
        // 2) Caricali TUTTI nel default context
        var loadedAssemblies = new List<Assembly>();
        foreach (var dll in pluginDlls) {
            try {
                var asmName = AssemblyName.GetAssemblyName(dll);
                var asmVer = asmName.Version?.ToString() ?? "n/a";
                var fvi = FileVersionInfo.GetVersionInfo(dll);
                var fileVer = fvi.FileVersion ?? "n/a";
                var prodVer = fvi.ProductVersion ?? "n/a";

                var asm = AssemblyLoadContext.Default
                             .LoadFromAssemblyPath(dll);
                loadedAssemblies.Add(asm);
                _initializationErrors.Enqueue(new LogErrorEntry {
                    Timestamp = DateTime.UtcNow,
                    SinkName = Path.GetFileNameWithoutExtension(dll),
                    ErrorMessage = $"[DBG] Loaded in DEFAULT context",
                    ContextInfo = baseDir
                });

                var infoVer = asm
            .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?
            .InformationalVersion ?? "n/a";

                Console.WriteLine(
            $"[DBG AddDynamicSinks] Loaded {Path.GetFileName(dll)} | " +
            $"AssemblyVersion={asmVer} | FileVersion={fileVer} | " +
            $"ProductVersion={prodVer} | InformationalVersion={infoVer} | " +
            $"context=DEFAULT");
            } catch (Exception ex) {
                _initializationErrors.Enqueue(new LogErrorEntry {
                    Timestamp = DateTime.UtcNow,
                    SinkName = Path.GetFileNameWithoutExtension(dll),
                    ErrorMessage = ex.Message,
                    ContextInfo = baseDir
                });
            }
        }

        var pluginTypes = loadedAssemblies
          .SelectMany(a => {
              try { return a.GetTypes(); } catch (Exception ex) {
                  _initializationErrors.Enqueue(new LogErrorEntry {
                      Timestamp = DateTime.UtcNow,
                      SinkName = a.FullName,
                      ErrorMessage = ex.Message,
                      ContextInfo = baseDir
                  });
                  return Array.Empty<Type>();
              }
          })
          .Where(t =>
              typeof(ISinkPlugin).IsAssignableFrom(t) &&
              !t.IsInterface &&
              !t.IsAbstract
          )
          .ToList();

        Console.WriteLine($"[dbg AddDynamicSinks] Found {pluginTypes.Count} ISinkPlugin implementations");

        if (!pluginTypes.Any())
            _initializationErrors.Enqueue(new LogErrorEntry {
                Timestamp = DateTime.UtcNow,
                SinkName = "Init",
                ErrorMessage = "No Sink loaded from reflection",
                ContextInfo = baseDir
            });


        foreach (var t in pluginTypes) {
            try {
                var instance = (ISinkPlugin)Activator.CreateInstance(t)!;
                SinkPluginRegistry.Register(instance);

                Console.WriteLine($"[dbg AddDynamicSinks] Registered plugin for sink: {t.Name}");
            } catch (Exception ex) {
                Console.WriteLine($"[err AddDynamicSinks] Exception loading plugin {t.Name}: {ex.Message}");
                _Errors.Add(
                    new LogErrorEntry {
                        Timestamp = DateTime.UtcNow,
                        SinkName = t.Name,
                        ErrorMessage = ex.Message,
                        ContextInfo = AppContext.BaseDirectory
                    });
                SinkNameInError = t.Name;
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
                if (_excludeSinkFile && condition.Sink.Equals("File", StringComparison.OrdinalIgnoreCase)) {
                    continue;
                }

                try {
                    plugin.HandleSink(_config, condition, _serilogConfig);
                    SinksLoaded.Add(new LoadedSinkInfo {
                        SinkName = condition.Sink!,
                        Levels = condition.Level!.ToList()
                    });

                    Console.WriteLine($"[dbg AddDynamicSinks] Added sink {condition.Sink} for levels {string.Join(", ", condition.Level)}");
                } catch (Exception ex) {
                    Console.WriteLine($"[err AddDynamicSinks] Exception configuring sink {condition.Sink}: {ex.Message}");
                    SelfLog.WriteLine($"Exception {ex.Message} on sink {condition.Sink}");
                }
            }
        }
        return this;
    }
}
