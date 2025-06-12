using Serilog;
using System.Runtime.CompilerServices;

namespace CSharpEssentials.LoggerHelper.Sink.Email;
internal class EmailSinkPlugin : ISinkPlugin {
    // Determines if this plugin should handle the given sink name
    public bool CanHandle(string sinkName) => sinkName == "Email";
    // Applies the MSSqlServer sink configuration to the LoggerConfiguration
    public void HandleSink(LoggerConfiguration loggerConfig, SerilogCondition condition, SerilogConfiguration serilogConfig) {
        var opts = serilogConfig.SerilogOption.MSSqlServer;

        loggerConfig.WriteTo.Conditional(
                                evt => serilogConfig.IsSinkLevelMatch(condition.Sink, evt.Level),
                                wt => wt.Sink(new CustomEmailSink(
                                    smtpServer: serilogConfig.SerilogOption?.Email.Host,
                                    smtpPort: (int)serilogConfig.SerilogOption?.Email.Port,
                                    fromEmail: serilogConfig.SerilogOption?.Email.From,
                                    toEmail: string.Join(",", serilogConfig.SerilogOption?.Email.To),
                                    username: serilogConfig.SerilogOption?.Email.username,
                                    password: serilogConfig.SerilogOption?.Email.password,
                                    serilogConfig.SerilogOption.Email?.ThrottleInterval ?? TimeSpan.FromSeconds(0),
                                    subjectPrefix: "[LoggerHelper]",
                                    enableSsl: (bool)serilogConfig.SerilogOption?.Email.EnableSsl,
                                    templatePath: serilogConfig.SerilogOption?.Email.TemplatePath
                                )));
    }
}
// Static initializer to auto-register the plugin when the assembly is loaded
public static class PluginInitializer {
    // This method is executed at module load time (requires .NET 5+ / C# 9+)
    [ModuleInitializer]
    public static void Init() {
        // Register this MSSqlServer plugin in the central registry
        SinkPluginRegistry.Register(new EmailSinkPlugin());
    }
}