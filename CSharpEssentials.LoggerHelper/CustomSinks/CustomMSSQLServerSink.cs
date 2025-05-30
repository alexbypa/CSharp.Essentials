//using Serilog.Sinks.MSSqlServer;
//using System.Data;

//namespace CSharpEssentials.LoggerHelper.CustomSinks;
///// <summary>
///// Helper class to generate ColumnOptions dynamically for MSSQL Sink,
///// including custom application fields such as IdTransaction, Action, etc.
///// </summary>
//internal static class CustomMSSQLServerSink {
//    /// <summary>
//    /// Builds a ColumnOptions object using the provided MSSqlServer config section,
//    /// dynamically including additional columns as specified in configuration.
//    /// </summary>
//    /// <param name="config">The configuration object binding MSSqlServer options from appsettings.</param>
//    /// <returns>ColumnOptions ready to be passed to Serilog MSSqlServer sink</returns>
//    internal static ColumnOptions GetColumnsOptions_v2(MSSqlServer config) {
//        var columnOptions = new ColumnOptions();
//        var additionalColumns = config.additionalColumns ?? Array.Empty<string>();

//        if (additionalColumns.Length > 0)
//            columnOptions.AdditionalColumns = new List<SqlColumn>();

//        foreach (var col in additionalColumns) {
//            columnOptions.AdditionalColumns.Add(new SqlColumn {
//                ColumnName = col,
//                DataType = SqlDbType.NVarChar,
//                AllowNull = true
//            });
//        }
//        return columnOptions;
//    }
//}