using MySqlConnector;
using Serilog.Debugging;
using Serilog.Events;
using Serilog.Formatting.Json;
using Serilog.Sinks.PeriodicBatching;
using System.Text;
using System.Text.Json;

namespace CSharpEssentials.LoggerHelper.Sink.MySql;

/// <summary>
/// Writes batches of log events to MySQL/MariaDB over MySqlConnector.
/// Columns are configurable, so the same structured fields exposed by the PostgreSQL sink
/// (ApplicationName, IdTransaction, Action, ...) land in real, queryable columns.
/// </summary>
internal sealed class MySqlBatchedSink : IBatchedLogEventSink {
    private readonly string _connectionString;
    private readonly IReadOnlyList<MySqlColumn> _columns;
    private readonly bool _storeTimestampInUtc;
    private readonly string _insertSql;
    private readonly string? _createTableSql;
    private readonly SemaphoreSlim _schemaGate = new(1, 1);
    private readonly JsonFormatter _eventFormatter = new(renderMessage: true);
    private bool _schemaChecked;

    internal MySqlBatchedSink(
        string connectionString,
        string tableName,
        IReadOnlyList<MySqlColumn> columns,
        bool autoCreateTable,
        bool addAutoIncrementColumn,
        bool storeTimestampInUtc) {

        MySqlColumnMap.EnsureValidIdentifier(tableName, "table name");
        if (columns.Count == 0)
            throw new InvalidOperationException("MySQL sink: at least one column must be configured.");

        _connectionString = connectionString;
        _columns = columns;
        _storeTimestampInUtc = storeTimestampInUtc;

        var quotedTable = MySqlColumnMap.Quote(tableName);
        var columnList = string.Join(", ", _columns.Select(c => MySqlColumnMap.Quote(c.Name)));
        var paramList = string.Join(", ", _columns.Select((_, i) => $"@p{i}"));
        _insertSql = $"INSERT INTO {quotedTable} ({columnList}) VALUES ({paramList});";

        _createTableSql = autoCreateTable
            ? BuildCreateTableSql(quotedTable, addAutoIncrementColumn)
            : null;
    }

    private string BuildCreateTableSql(string quotedTable, bool addAutoIncrementColumn) {
        var sb = new StringBuilder($"CREATE TABLE IF NOT EXISTS {quotedTable} (");

        if (addAutoIncrementColumn)
            sb.Append("`Id` BIGINT NOT NULL AUTO_INCREMENT PRIMARY KEY, ");

        sb.Append(string.Join(", ", _columns.Select(c => $"{MySqlColumnMap.Quote(c.Name)} {c.SqlType} NULL")));
        sb.Append(") CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci;");
        return sb.ToString();
    }

    public async Task EmitBatchAsync(IEnumerable<LogEvent> batch) {
        var events = batch as IReadOnlyCollection<LogEvent> ?? batch.ToList();
        if (events.Count == 0)
            return;

        try {
            await using var connection = new MySqlConnection(_connectionString);
            await connection.OpenAsync().ConfigureAwait(false);

            await EnsureSchemaAsync(connection).ConfigureAwait(false);

            await using var transaction = await connection.BeginTransactionAsync().ConfigureAwait(false);
            await using var command = new MySqlCommand(_insertSql, connection, transaction);

            for (var i = 0; i < _columns.Count; i++)
                command.Parameters.Add(new MySqlParameter($"@p{i}", MySqlDbType.VarString));

            foreach (var logEvent in events) {
                for (var i = 0; i < _columns.Count; i++)
                    command.Parameters[i].Value = ExtractValue(_columns[i], logEvent) ?? DBNull.Value;

                await command.ExecuteNonQueryAsync().ConfigureAwait(false);
            }

            await transaction.CommitAsync().ConfigureAwait(false);
        }
        catch (Exception ex) {
            // A sink must never take the host process down; report through Serilog's own diagnostics.
            SelfLog.WriteLine($"MySQL sink: failed to write batch of {events.Count} events: {ex}");
        }
    }

    public Task OnEmptyBatchAsync() => Task.CompletedTask;

    private async Task EnsureSchemaAsync(MySqlConnection connection) {
        if (_schemaChecked || _createTableSql is null)
            return;

        await _schemaGate.WaitAsync().ConfigureAwait(false);
        try {
            if (_schemaChecked)
                return;

            await using var command = new MySqlCommand(_createTableSql, connection);
            await command.ExecuteNonQueryAsync().ConfigureAwait(false);
            _schemaChecked = true;
        }
        finally {
            _schemaGate.Release();
        }
    }

    private object? ExtractValue(MySqlColumn column, LogEvent logEvent) => column.Writer switch {
        MySqlWriterKind.Rendered => logEvent.RenderMessage(),
        MySqlWriterKind.Template => logEvent.MessageTemplate.Text,
        MySqlWriterKind.Level => logEvent.Level.ToString(),
        MySqlWriterKind.Timestamp => _storeTimestampInUtc ? logEvent.Timestamp.UtcDateTime : logEvent.Timestamp.LocalDateTime,
        MySqlWriterKind.Exception => logEvent.Exception?.ToString(),
        MySqlWriterKind.Serialized => FormatEvent(logEvent),
        MySqlWriterKind.Properties => FormatProperties(logEvent),
        MySqlWriterKind.Single => ReadProperty(logEvent, column.Property!),
        _ => null
    };

    private string FormatEvent(LogEvent logEvent) {
        using var writer = new StringWriter();
        _eventFormatter.Format(logEvent, writer);
        return writer.ToString();
    }

    private static string FormatProperties(LogEvent logEvent) {
        var map = logEvent.Properties.ToDictionary(p => p.Key, p => Simplify(p.Value));
        return JsonSerializer.Serialize(map);
    }

    private static string? ReadProperty(LogEvent logEvent, string propertyName) {
        if (!logEvent.Properties.TryGetValue(propertyName, out var value))
            return null;

        // ScalarValue renders strings with surrounding quotes, which is not what a column wants.
        return value is ScalarValue { Value: { } scalar }
            ? scalar as string ?? scalar.ToString()
            : value.ToString();
    }

    /// <summary>Converts Serilog property values into plain CLR objects that System.Text.Json can render.</summary>
    private static object? Simplify(LogEventPropertyValue value) => value switch {
        ScalarValue scalar => scalar.Value,
        SequenceValue sequence => sequence.Elements.Select(Simplify).ToList(),
        StructureValue structure => structure.Properties.ToDictionary(p => p.Name, p => Simplify(p.Value)),
        DictionaryValue dictionary => dictionary.Elements.ToDictionary(
            kvp => kvp.Key.Value?.ToString() ?? string.Empty,
            kvp => Simplify(kvp.Value)),
        _ => value.ToString()
    };
}
