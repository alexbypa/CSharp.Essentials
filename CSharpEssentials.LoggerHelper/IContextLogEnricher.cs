using Serilog;

namespace CSharpEssentials.LoggerHelper;
public interface IContextLogEnricher {
    ILogger Enrich(ILogger logger, object? context);
    LoggerConfiguration Enrich(LoggerConfiguration configuration);

}
