using Microsoft.Extensions.Configuration;
using Serilog.Sinks.PostgreSQL.ColumnWriters;
using Serilog.Sinks.PostgreSQL;
using NpgsqlTypes;

namespace CSharpEssentials.LoggerHelper;
    public class PostgreSQLOptions {
    public static Dictionary<string, ColumnWriterBase> BuildPostgresColumns(IConfiguration config) {
        var colDefs = config.GetSection("Serilog:SerilogConfiguration:SerilogOption:PostgreSQL:ColumnsPostGreSQL").Get<List<ColumnsPostGreSQL>>();
        var result = new Dictionary<string, ColumnWriterBase>();
        
        //// 💡 Inject default Id column if not defined
        //if (colDefs.All(c => !string.Equals(c.Name, "Id", StringComparison.OrdinalIgnoreCase))) {
        //    result["Id"] = new RawSqlColumnWriter("Id SERIAL PRIMARY KEY");
        //} // TODO:
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

        return result;
    }
    private static NpgsqlDbType GetNpgsqlDbType(string type) =>
        type.ToLower() switch {
            "text" => NpgsqlDbType.Text,
            "jsonb" => NpgsqlDbType.Jsonb,
            "varchar" => NpgsqlDbType.Varchar,
            "timestamp" => NpgsqlDbType.Timestamp,
            _ => NpgsqlDbType.Text
        };

}
