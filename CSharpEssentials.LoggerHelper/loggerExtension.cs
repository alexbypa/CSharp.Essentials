using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;
using Serilog.Events;
using System.Text.RegularExpressions;
using System.Diagnostics;
using Org.BouncyCastle.Asn1.Ocsp;

#if NET6_0
using Microsoft.AspNetCore.Builder;
#endif

namespace CSharpEssentials.LoggerHelper;
public interface ILoggerRequest {
    public string IdTransaction { get; }
    public string Action { get; }
    public string ApplicationName { get; }
}
public interface IRequest : ILoggerRequest { }

public static class LoggerExtensionConfig {
#if NET6_0
    public static IServiceCollection AddLoggerConfiguration(this WebApplicationBuilder builder) {
        var externalConfigPath = Path.Combine(Directory.GetCurrentDirectory(), "appsettings.LoggerHelper.json");
        if (File.Exists(externalConfigPath)) {
            builder.Configuration.AddJsonFile(externalConfigPath, optional: true, reloadOnChange: true);
        }
        return builder.Services;
    }
#else
    public static IServiceCollection AddloggerConfiguration(this IServiceCollection services, IHostApplicationBuilder builder) {
        var externalConfigPath = Path.Combine(Directory.GetCurrentDirectory(), "appsettings.LoggerHelper.json");
        if (File.Exists(externalConfigPath)) {
            builder.Configuration.AddJsonFile(externalConfigPath, optional: true, reloadOnChange: true);
        }
        return services;
    }
#endif
}
public class loggerExtension<T> where T : IRequest {
    //TODO: Riprendere le altre tipologie di estensione Enrich etc etc json ....
    public static readonly ILogger log;
    public static string postGreSQLConnectionString = "";

    static loggerExtension() {
        var configuration = new ConfigurationBuilder()
#if DEBUG
        .AddJsonFile("appsettings.LoggerHelper.debug.json")
#else
        .AddJsonFile("appsettings.LoggerHelper.json")
#endif
        .Build();
        var builder = new LoggerBuilder(configuration).AddDynamicSinks();
        log = builder.Build();

        var enricher = ServiceLocator.GetService<IContextLogEnricher>();
        log = enricher != null
               ? enricher.Enrich(log, context: null)
               : log;
    }
    /// <summary>
    /// method to write log
    /// </summary>
    /// <param name="Action">Action is the parameter that indicates the area of ​​interest in which the log is being written</param>
    /// <param name="IdTransaction">IdTransaction is the reference code for retrieving the log (for example, if you use a warehouse program, each operation on a product could be the barcode of the item)</param>
    /// <param name="level">level is the LogEventLevel of serilog</param>
    /// <param name="ex">is the text of the error to report</param>
    /// <param name="message">indicates the log message</param>
    /// <param name="args">are additional parameters that can help identify particular scenarios</param>
    public static async void TraceAsync(IRequest request, LogEventLevel level, Exception? ex, string message, params object[] args) {
        await Task.Run(() => TraceSync(request, level, ex, message, args));
    }
    /// <summary>
    /// method to write log
    /// </summary>
    /// <param name="Action">Action is the parameter that indicates the area of ​​interest in which the log is being written</param>
    /// <param name="IdTransaction">IdTransaction is the reference code for retrieving the log (for example, if you use a warehouse program, each operation on a product could be the barcode of the item)</param>
    /// <param name="level">level is the LogEventLevel of serilog</param>
    /// <param name="ex">is the text of the error to report</param>
    /// <param name="message">indicates the log message</param>
    /// <param name="args">are additional parameters that can help identify particular scenarios</param>
    public static void TraceSync(IRequest request, LogEventLevel level, Exception? ex, string message, params object[] args) {
        message += " {IdTransaction} {MachineName} {Action}";
        List<object> arguments = new List<object>();
        if (args != null)
            arguments = args.ToList();

        var IdTransaction = request?.IdTransaction ?? Guid.NewGuid().ToString();
        var Action = request?.Action ?? "UNKNOWN";
        arguments.Add(IdTransaction);
        arguments.Add(Environment.MachineName);
        arguments.Add(Action);

        var spanName = Activity.Current?.DisplayName;
        var logger = log;
        if (!string.IsNullOrEmpty(spanName))
            logger = log.ForContext("SpanName", spanName);

        int totPlaceHolders = arguments.Count;
        try {
            var matches = Regex.Matches(message, @"(?<!\{)\{[a-zA-Z_][a-zA-Z0-9_]*\}(?!\})", RegexOptions.None, TimeSpan.FromMilliseconds(200));
            totPlaceHolders = matches.Count;
            if (totPlaceHolders != arguments.Count) {
                logger.Warning("LoggerHelper: Placeholder count mismatch. MessageTemplate: {Message}, Expected: {Expected}, Actual: {Actual}",
                message, matches.Count, arguments.Count);
            }
        } catch (Exception exRegEx) {
            logger.Warning("LoggerHelper: Regex failed to validate placeholders: {Error}", exRegEx.Message);
        }
        var enricher = ServiceLocator.GetService<IContextLogEnricher>();
        if (enricher != null)
            logger = enricher.Enrich(logger, request);

        logger.Write(level, ex, message, arguments.ToArray());
    }
}
