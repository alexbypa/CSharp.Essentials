using Serilog.Sinks.MSSqlServer;
using System.Data;

namespace CSharpEssentials.LoggerHelper.Sink.MSSqlServer;
internal static class CustomMSSQLServerSink {
    /// <summary>
    /// Builds a ColumnOptions object using the provided MSSqlServer config section,
    /// dynamically including additional columns as specified in configuration.
    /// </summary>
    /// <param name="config">The configuration object binding MSSqlServer options from appsettings.</param>
    /// <returns>ColumnOptions ready to be passed to Serilog MSSqlServer sink</returns>
    internal static ColumnOptions GetColumnsOptions_v2(LoggerHelper.MSSqlServer config) {
        var columnOptions = new ColumnOptions();

        if (columnOptions.Store != null)
            columnOptions.Store.Clear();

        // 1. Configura colonne standard
        var stdCols = config?.columnOptionsSection?.addStandardColumns;
        if (stdCols != null) {
            foreach (var col in stdCols) {
                if (Enum.TryParse<StandardColumn>(col, out var parsed))
                    columnOptions.Store.Add(parsed);
            }
        }
        // 2. Rimuove colonne standard
        var toRemove = config?.columnOptionsSection?.removeStandardColumns;
        if (toRemove != null) {
            foreach (var col in toRemove) {
                if (Enum.TryParse<StandardColumn>(col, out var parsed))
                    columnOptions.Store.Remove(parsed);
            }
        }
        var additionalColumns = config.additionalColumns ?? new List<AdditionalSqlColumnDto>();

        if (additionalColumns.Count > 0)
            columnOptions.AdditionalColumns = new List<SqlColumn>();

        foreach (var col in additionalColumns) {
            columnOptions.AdditionalColumns.Add(new SqlColumn {
                ColumnName = col.ColumnName,
                DataType = ParseSqlDbType(col.DataType),
                AllowNull = col.AllowNull,
                DataLength = col.DataLength
            });
        }
        return columnOptions;
    }
    private static SqlDbType ParseSqlDbType(string typeName) {
        return Enum.TryParse<SqlDbType>(typeName, true, out var parsed)
            ? parsed
            : SqlDbType.NVarChar;
    }
}