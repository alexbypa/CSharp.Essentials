namespace CSharpEssentials.LoggerHelper;
public class SerilogConfiguration {
    public string? ApplicationName { get; set; }
    public List<SerilogCondition>? SerilogCondition { get; set; }
    public SerilogOption? SerilogOption { get; set; }
}
public class SerilogCondition {
    public string? Sink { get; set; }
    public List<string>? Level { get; set; }
}
public class SerilogOption {
    public MSSqlServer? MSSqlServer { get; set; }
    public PostgreSQL? PostgreSQL { get; set; }
    public TelegramOption? TelegramOption { get; set; }
    public SinkFileOptions? File { get; set; }
    public GeneralConfig? GeneralConfig { get; set; }
    public ElasticSearch? ElasticSearch { get; set; }
    public Email? Email { get; set; }
}
public class Email {
    public string? From { get; set; }
    public int? Port { get; set; }
    public string? Host { get; set; }
    public string To { get; set; }
    public string? password { get; set; }
    public string? username { get; set; }
    public bool EnableSsl { get; set; }
    public string? TemplatePath { get; set; }
}
public class PostgreSQL {
    public string? connectionstring { get; set; }
    public string tableName { get; set; }
    public string schemaName { get; set; }
    public bool needAutoCreateTable { get; set; }
    public bool addAutoIncrementColumn { get; set; }
    public ColumnsPostGreSQL[] ColumnsPostGreSQL { get; set; }
}
public class ColumnsPostGreSQL {
    public string Name { get; set; }
    public string Writer { get; set; }
    public string Type { get; set; } // es: "Text", "Jsonb", "Timestamp"
    public string Property { get; set; } // solo per SinglePropertyColumnWriter
}


public class ElasticSearch {
    public string? nodeUris { get; set; }
    public string? indexFormat { get; set; }
}
public class GeneralConfig {
    public bool EnableSelfLogging { get; set; }
}
public class MSSqlServer {
    public string? connectionString { get; set; }
    public SinkMsSqlOptionsSection? sinkOptionsSection { get; set; }
    public string[] additionalColumns { get; set; }
}
public class SinkMsSqlOptionsSection {
    public string? tableName { get; set; }
    public string? schemaName { get; set; }
    public bool autoCreateSqlTable { get; set; }
    public int batchPostingLimit { get; set; }
    public string? period { get; set; }
}
public class SinkFileOptions {
    public string? Path { get; set; }
    public string RollingInterval { get; set; }
    public int RetainedFileCountLimit { get; set; }
    public bool Shared { get; set; }
}
public class TelegramOption {
    public string? Api_Key { get; set; }
    public string? chatId { get; set; }
}