using System.Text.RegularExpressions;

namespace CSharpEssentials.LoggerHelper.Sink.MySql;

/// <summary>How a column value is extracted from a <see cref="Serilog.Events.LogEvent"/>.</summary>
internal enum MySqlWriterKind {
    Rendered,
    Template,
    Level,
    Timestamp,
    Exception,
    Serialized,
    Properties,
    Single
}

/// <summary>A resolved column: database name, extraction strategy and DDL type.</summary>
internal sealed record MySqlColumn(string Name, MySqlWriterKind Writer, string SqlType, string? Property);

/// <summary>
/// Translates <see cref="MySqlColumnConfig"/> entries into resolved columns, and supplies the
/// default column set (kept aligned with the PostgreSQL sink so both databases expose the same fields).
/// </summary>
internal static class MySqlColumnMap {
    /// <summary>MySQL identifiers are max 64 chars; restricting the charset keeps them safe to quote.</summary>
    private static readonly Regex IdentifierPattern = new(@"^[A-Za-z0-9_]{1,64}$", RegexOptions.Compiled);

    internal static IReadOnlyList<MySqlColumn> Default() => new List<MySqlColumn> {
        new("ApplicationName", MySqlWriterKind.Single, "VARCHAR(255)", "ApplicationName"),
        new("message", MySqlWriterKind.Rendered, "TEXT", null),
        new("message_template", MySqlWriterKind.Template, "TEXT", null),
        new("level", MySqlWriterKind.Level, "VARCHAR(32)", null),
        new("raise_date", MySqlWriterKind.Timestamp, "DATETIME(6)", null),
        new("exception", MySqlWriterKind.Exception, "TEXT", null),
        new("properties", MySqlWriterKind.Serialized, "JSON", null),
        new("MachineName", MySqlWriterKind.Single, "VARCHAR(255)", "MachineName"),
        new("Action", MySqlWriterKind.Single, "VARCHAR(255)", "Action"),
        new("IdTransaction", MySqlWriterKind.Single, "VARCHAR(255)", "IdTransaction")
    };

    internal static IReadOnlyList<MySqlColumn> FromConfig(List<MySqlColumnConfig> columnDefs) {
        var result = new List<MySqlColumn>(columnDefs.Count);

        foreach (var col in columnDefs) {
            EnsureValidIdentifier(col.Name, "column name");

            var writer = ParseWriter(col.Writer);
            var property = writer == MySqlWriterKind.Single ? (col.Property ?? col.Name) : null;
            result.Add(new MySqlColumn(col.Name, writer, ParseSqlType(col.Type, col.Length), property));
        }

        return result;
    }

    internal static void EnsureValidIdentifier(string value, string what) {
        if (!IdentifierPattern.IsMatch(value ?? string.Empty))
            throw new InvalidOperationException(
                $"MySQL sink: invalid {what} '{value}'. Use letters, digits and underscore only (max 64 chars).");
    }

    /// <summary>Wraps an already-validated identifier in backticks.</summary>
    internal static string Quote(string identifier) => $"`{identifier}`";

    private static MySqlWriterKind ParseWriter(string writer) => writer?.ToLowerInvariant() switch {
        "rendered" => MySqlWriterKind.Rendered,
        "template" => MySqlWriterKind.Template,
        "level" => MySqlWriterKind.Level,
        "timestamp" => MySqlWriterKind.Timestamp,
        "exception" => MySqlWriterKind.Exception,
        "serialized" => MySqlWriterKind.Serialized,
        "properties" => MySqlWriterKind.Properties,
        "single" => MySqlWriterKind.Single,
        _ => throw new InvalidOperationException(
            $"Writer '{writer}' not supported. Use: Rendered, Template, Level, Timestamp, Exception, Serialized, Properties, Single.")
    };

    private static string ParseSqlType(string type, int length) => type?.ToLowerInvariant() switch {
        "text" => "TEXT",
        "longtext" => "LONGTEXT",
        "json" => "JSON",
        "varchar" => $"VARCHAR({(length is > 0 and <= 16383 ? length : 255)})",
        "datetime" or "timestamp" => "DATETIME(6)",
        "int" => "INT",
        "bigint" => "BIGINT",
        "bool" or "boolean" => "TINYINT(1)",
        _ => "TEXT"
    };
}
