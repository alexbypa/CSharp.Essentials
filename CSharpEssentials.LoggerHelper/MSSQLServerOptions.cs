using Serilog.Sinks.MSSqlServer;
using System.Data;

namespace CSharpEssentials.LoggerHelper;
public class MSSQLServerOptions {
    public static ColumnOptions GetColumnOptions() {
        var columnOptions = new Serilog.Sinks.MSSqlServer.ColumnOptions();
        columnOptions.Store.Add(StandardColumn.LogEvent);
        columnOptions.AdditionalColumns = new List<SqlColumn> {
            new SqlColumn { DataType = SqlDbType.VarChar, ColumnName = "IdTransaction", DataLength = 250, AllowNull = false },
            new SqlColumn { DataType = SqlDbType.VarChar, ColumnName = "MachineName", DataLength = 250, AllowNull = false },
            new SqlColumn { DataType = SqlDbType.VarChar, ColumnName = "Action", DataLength = 250, AllowNull = false },
            new SqlColumn { DataType = SqlDbType.VarChar, ColumnName = "ApplicationName", DataLength = 250, AllowNull = false }
        };
        return columnOptions;
    }
}