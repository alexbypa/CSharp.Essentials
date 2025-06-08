using Npgsql;
using NpgsqlTypes;
using Serilog.Debugging;
using Serilog.Sinks.PostgreSQL;
using Serilog.Sinks.PostgreSQL.ColumnWriters;

namespace CSharpEssentials.LoggerHelper.Sink.Postgresql;
/// <summary>
/// Provides helper methods for PostgreSQL integration with Serilog, including automatic
/// table creation and optional auto-increment primary key column.
/// </summary>
internal class CustomPostgresQLSink {
    // Ensures the 'id' column is only added once
    private static bool _idColumnEnsured = false;
    /// <summary>
    /// Adds an auto-increment 'id' column as primary key if it doesn't already exist.
    /// </summary>
    private static void EnsurePrimaryKeyColumn(string connectionString, string table_name, string table_schema) {
        using var connection = new NpgsqlConnection(connectionString);
        connection.Open();

        using var cmd = connection.CreateCommand();
        cmd.CommandText = string.Format(@"
DO $$
BEGIN
    IF EXISTS (
        SELECT 1
        FROM information_schema.tables
        WHERE table_schema = '{0}'
          AND table_name = '{1}'
    ) AND NOT EXISTS (
        SELECT 1
        FROM information_schema.columns
        WHERE table_schema = '{0}'
          AND table_name = '{1}'
          AND lower(column_name) = 'id'
    ) THEN
        ALTER TABLE ""{0}"".""{1}""
        ADD COLUMN id SERIAL PRIMARY KEY;
    END IF;
END
$$;
", table_schema, table_name);
        cmd.ExecuteNonQuery();
    }

    /// <summary>
    /// Builds the dictionary of column writers used by Serilog to log to PostgreSQL.
    /// Automatically creates the target table and adds primary key if configured.
    /// </summary>
    public static async Task<Dictionary<string, ColumnWriterBase>> BuildPostgresColumns(SerilogConfiguration config) {
        var colDefs = config.SerilogOption?.PostgreSQL?.ColumnsPostGreSQL;
        var result = new Dictionary<string, ColumnWriterBase>();
        if (colDefs != null)
            foreach (var col in colDefs) {
                var dbType = GetNpgsqlDbType(col.Type ?? "Text");
                result[col.Name] = col.Writer switch {
                    "Rendered" => new RenderedMessageColumnWriter(dbType),
                    "Template" => new MessageTemplateColumnWriter(dbType),
                    "Level" => new LevelColumnWriter(true, dbType),
                    "timestamp" => new TimestampColumnWriter(dbType),
                    "Exception" => new ExceptionColumnWriter(dbType),
                    "Serialized" => new LogEventSerializedColumnWriter(dbType),
                    "Properties" => new PropertiesColumnWriter(dbType),
                    "Single" => new SinglePropertyColumnWriter(
                                        col.Property ?? col.Name,
                                        PropertyWriteMethod.ToString,
                                        dbType),
                    _ => throw new InvalidOperationException($"Writer '{col.Writer}' non supportato.")
                };
            }
        else
            result = AddDefaultColumns();
        try {
            if (config.SerilogOption != null && config.SerilogOption.PostgreSQL != null && config.SerilogOption.PostgreSQL.addAutoIncrementColumn) {
                string connectionString = config.SerilogOption.PostgreSQL.connectionstring ?? string.Empty;
                string schemaName = config.SerilogOption?.PostgreSQL?.schemaName ?? "public";
                string tableName = config.SerilogOption?.PostgreSQL?.tableName ?? "LogEvents";

                if (!string.IsNullOrEmpty(connectionString) && !_idColumnEnsured && result != null) {
                    await CreateTable(connectionString, schemaName, tableName, result);
                    EnsurePrimaryKeyColumn(connectionString, tableName, schemaName);
                    _idColumnEnsured = true;
                }
            }
        } catch (Exception ex) {
            SelfLog.WriteLine($"Error ensuring primary key column: {ex.Message}");
        }

        return result;
    }
    private static Dictionary<string, ColumnWriterBase> AddDefaultColumns() {
        return new Dictionary<string, ColumnWriterBase>{
            {"ApplicationName", new SinglePropertyColumnWriter("ApplicationName", PropertyWriteMethod.ToString, NpgsqlDbType.Text) },
            {"message", new RenderedMessageColumnWriter(NpgsqlDbType.Text) },
            {"message_template", new MessageTemplateColumnWriter(NpgsqlDbType.Text) },
            {"level", new LevelColumnWriter(true, NpgsqlDbType.Varchar) },
            {"raise_date", new TimestampColumnWriter(NpgsqlDbType.Timestamp) },
            {"exception", new ExceptionColumnWriter(NpgsqlDbType.Text) },
            {"properties", new LogEventSerializedColumnWriter(NpgsqlDbType.Jsonb) },
            {"props_test", new PropertiesColumnWriter(NpgsqlDbType.Jsonb) },
            {"MachineName", new SinglePropertyColumnWriter("MachineName", PropertyWriteMethod.ToString, NpgsqlDbType.Text, "l") },
            {"Action", new SinglePropertyColumnWriter("Action", PropertyWriteMethod.ToString, NpgsqlDbType.Text) },
            {"IdTransaction", new SinglePropertyColumnWriter("IdTransaction", PropertyWriteMethod.ToString, NpgsqlDbType.Text) }
        };
    }
    /// <summary>
    /// Invokes the external TableCreator to create the table with the given column definitions.
    /// </summary>
    private static async Task CreateTable(string connectionstring, string schemaName, string tableName, Dictionary<string, ColumnWriterBase> columnsInfo) {
        using var conn = new NpgsqlConnection(connectionstring);
        await conn.OpenAsync();
        await TableCreator.CreateTable(conn, schemaName, tableName, columnsInfo);
    }
    /// <summary>
    /// Maps string types to NpgsqlDbType.
    /// </summary>
    private static NpgsqlDbType GetNpgsqlDbType(string type) =>
        type.ToLower() switch {
            "text" => NpgsqlDbType.Text,
            "jsonb" => NpgsqlDbType.Jsonb,
            "varchar" => NpgsqlDbType.Varchar,
            "timestamp" => NpgsqlDbType.Timestamp,
            _ => NpgsqlDbType.Text
        };
}
