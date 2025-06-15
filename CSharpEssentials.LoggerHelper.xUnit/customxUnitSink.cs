using Serilog.Core;
using Serilog.Events;
using Xunit.Abstractions;

namespace CSharpEssentials.LoggerHelper.xUnit;
public class customxUnitSink : ILogEventSink {
    public void Emit(LogEvent logEvent) {
        var output = XUnitTestOutputHelperStore.Output;
        if (output == null)
            return;

        var writer = new StringWriter();
        logEvent.RenderMessage(writer);
        output.WriteLine(writer.ToString());
    }
}
