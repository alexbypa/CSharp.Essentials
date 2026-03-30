using Npgsql;
using Serilog;
using Serilog.Debugging;
using System.Runtime.CompilerServices;
using System.Threading;

namespace CSharpEssentials.LoggerHelper.Sink.Postgresql;
    internal class PostGreSinkPlugin : ISinkPlugin {
    // Determines if this plugin should handle the given sink name
    public bool CanHandle(string sinkName) => sinkName == "PostgreSQL";
    // Applies the MSSqlServer sink configuration to the LoggerConfiguration
    public void HandleSink(LoggerConfiguration loggerConfig, SerilogCondition condition, SerilogConfiguration serilogConfig) {
        if (serilogConfig.SerilogOption == null || serilogConfig.SerilogOption.PostgreSQL == null) {
            SelfLog.WriteLine($"Configuration exception : section PostgreSQL missing on Serilog:SerilogConfiguration:SerilogOption https://github.com/alexbypa/CSharp.Essentials/blob/TestLogger/LoggerHelperDemo/LoggerHelperDemo/Readme.md#installation");
            return;
        }

        var connectionString = serilogConfig.SerilogOption.PostgreSQL.connectionstring;
        try {
            var csb = new NpgsqlConnectionStringBuilder(connectionString) { Timeout = 3 };
            using var testConn = new NpgsqlConnection(csb.ConnectionString);
            testConn.Open();
            testConn.Close();
        } catch (Exception ex) {
            var csb = new NpgsqlConnectionStringBuilder(connectionString);
            var target = $"Host={csb.Host};Port={csb.Port};Database={csb.Database};Username={csb.Username}";
            var inner = ex.InnerException != null ? $" | InnerException: [{ex.InnerException.GetType().Name}] {ex.InnerException.Message}" : string.Empty;
            var (category, hint) = ClassifyPostgresException(ex);
            SelfLog.WriteLine($"[PostgreSQL sink skipped] CATEGORY={category} | TARGET={target} | [{ex.GetType().Name}] {ex.Message}{inner} | HINT={hint}");
            return;
        }

        try {
            loggerConfig.WriteTo.Conditional(
                            evt => serilogConfig.IsSinkLevelMatch(condition.Sink, evt.Level),
                            wt => {
                                wt.PostgreSQL(
                                        connectionString: connectionString,
                                        tableName: serilogConfig.SerilogOption.PostgreSQL.tableName,
                                        schemaName: serilogConfig.SerilogOption.PostgreSQL.schemaName,
                                        needAutoCreateTable: true,
                                        columnOptions: CustomPostgresQLSink.BuildPostgresColumns(serilogConfig).GetAwaiter().GetResult()
                                    );
                                }
                            );
        }catch (Exception ex) {
            SelfLog.WriteLine($"Error HandleSink on PostgreSQL: {ex.Message}");
        }

    }
    private static (string category, string hint) ClassifyPostgresException(Exception ex) {
        var msg = (ex.Message + " " + ex.InnerException?.Message).ToLowerInvariant();

        if (msg.Contains("connection refused") || msg.Contains("forcibly closed") || msg.Contains("reading from stream") || msg.Contains("no such host"))
            return ("INFRASTRUCTURE", "The PostgreSQL server is unreachable. Check that the server is running, the host/port are correct, and that no firewall is blocking port 5432. This is NOT an application bug.");

        if (msg.Contains("password") || msg.Contains("authentication") || msg.Contains("pg_hba"))
            return ("CREDENTIALS", "Authentication failed. Verify the username and password in the connection string, and check pg_hba.conf on the server. This is NOT an application bug.");

        if (msg.Contains("timeout") || msg.Contains("timed out"))
            return ("NETWORK_TIMEOUT", "Connection timed out. The server may be overloaded or a firewall is silently dropping packets to port 5432. This is NOT an application bug.");

        if (msg.Contains("database") && (msg.Contains("does not exist") || msg.Contains("not found")))
            return ("CONFIGURATION", "The target database does not exist. Verify the database name in the connection string. This is NOT an application bug.");

        if (msg.Contains("ssl") || msg.Contains("certificate"))
            return ("SSL", "SSL/TLS handshake failed. Check SSL configuration on the server and client. This is NOT an application bug.");

        return ("UNKNOWN", "An unexpected error occurred while connecting to PostgreSQL. Forward the full error details to the development team.");
    }
}
// Static initializer to auto-register the plugin when the assembly is loaded
public static class PluginInitializer {
    // This method is executed at module load time (requires .NET 5+ / C# 9+)
    [ModuleInitializer]
    public static void Init() {
        // Register this MSSqlServer plugin in the central registry
        SinkPluginRegistry.Register(new PostGreSinkPlugin());
    }
}