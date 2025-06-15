using Serilog.Core;
using Serilog.Events;

namespace CSharpEssentials.LoggerHelper.xUnit;
public class customxUnitSink : ILogEventSink {
    public void Emit(LogEvent logEvent) {
        var output = XUnitTestOutputHelperStore.Output;
        if (output == null)
            return;

        var writer = new StringWriter();
        logEvent.RenderMessage(writer);

        var action = logEvent.Properties.TryGetValue("Action", out var actionVal)
        ? actionVal.ToString().Trim('"') // rimuove le virgolette di ToString()
        : "n/a";

        string levelText = logEvent.Level switch {
            LogEventLevel.Information => "[ℹ️ Info]",
            LogEventLevel.Warning => "[⚠️ Warning]",
            LogEventLevel.Error => "[❌ Error]",
            LogEventLevel.Fatal => "[💀 Fatal]",
            LogEventLevel.Debug => "[🐛 Debug]",
            LogEventLevel.Verbose => "[🔍 Verbose]",
            _ => $"[{logEvent.Level}]"
        };

        var exceptionText = logEvent.Exception != null
        ? $" EX: {logEvent.Exception.GetType().Name} - {logEvent.Exception.Message}"
        : "";

        output.WriteLine($"{levelText} [{action}] {writer} {exceptionText}");
    }
}
