using Xunit.Abstractions;
using CSharpEssentials.LoggerHelper.Sink.MySql;
using System.Reflection;
using System.Text.Json;
using Serilog.Events;
using Serilog.Parsing;

namespace CSharpEssentials.LoggerHelper.Tests;

public class MySqlBatchedSinkTests {
    private readonly ITestOutputHelper _output;

    public MySqlBatchedSinkTests(ITestOutputHelper output) {
        _output = output;
    }

    private static readonly IReadOnlyList<MySqlColumn> TwoCols = [
        new("a", MySqlWriterKind.Rendered, "TEXT", null),
        new("b", MySqlWriterKind.Level, "VARCHAR(32)", null)
    ];

    private static MySqlBatchedSink Create(IReadOnlyList<MySqlColumn>? cols = null, string table = "logs",
        bool autoCreate = true, bool autoId = false, bool utc = true) =>
        new("Server=none", table, cols ?? TwoCols, autoCreate, autoId, utc);

    // Generated SQL is private state; read via reflection to keep production surface unchanged.
    private static string? Field(MySqlBatchedSink sink, string name) =>
        (string?)typeof(MySqlBatchedSink).GetField(name, BindingFlags.NonPublic | BindingFlags.Instance)!.GetValue(sink);

    [Theory]
    [InlineData("t`x")]
    [InlineData("t;x")]
    [InlineData("t x")]
    [InlineData("t\n")]
    [InlineData(null)]
    public void Ctor_InvalidTableName_ThrowsInvalidOperationException(string? table) {
        _output.WriteLine($"[Scenario] Costruttore di MySqlBatchedSink con nome tabella non valido \"{table ?? "null"}\" (backtick, punto e virgola, spazio o null)");
        _output.WriteLine("[Atteso] InvalidOperationException: il nome tabella viene validato prima di costruire l'SQL (protezione SQL injection)");

        var ex = Record.Exception(() => Create(table: table!));

        _output.WriteLine($"[Restituito] Eccezione: {ex?.GetType().Name ?? "nessuna"}");
        Assert.IsType<InvalidOperationException>(ex);
    }

    [Fact]
    public void Ctor_TableName65Chars_Throws() {
        _output.WriteLine("[Scenario] Costruttore con nome tabella di 65 caratteri (oltre il limite MySQL di 64)");
        _output.WriteLine("[Atteso] InvalidOperationException: nome troppo lungo");

        var ex = Record.Exception(() => Create(table: new string('t', 65)));

        _output.WriteLine($"[Restituito] Eccezione: {ex?.GetType().Name ?? "nessuna"}");
        Assert.IsType<InvalidOperationException>(ex);
    }

    [Fact]
    public void Ctor_NoColumns_ThrowsInvalidOperationException() {
        _output.WriteLine("[Scenario] Costruttore con lista di colonne vuota");
        _output.WriteLine("[Atteso] InvalidOperationException: servono almeno una colonna per generare l'INSERT");

        var ex = Record.Exception(() => Create(cols: []));

        _output.WriteLine($"[Restituito] Eccezione: {ex?.GetType().Name ?? "nessuna"}");
        Assert.IsType<InvalidOperationException>(ex);
    }

    [Fact]
    public void Ctor_ValidColumns_BuildsParameterizedInsert() {
        _output.WriteLine("[Scenario] Costruttore con tabella \"logs\" e 2 colonne (a, b)");
        _output.WriteLine("[Atteso] INSERT parametrizzata: \"INSERT INTO `logs` (`a`, `b`) VALUES (@p0, @p1);\"");

        var sql = Field(Create(), "_insertSql");

        _output.WriteLine($"[Restituito] {sql}");
        Assert.Equal("INSERT INTO `logs` (`a`, `b`) VALUES (@p0, @p1);", sql);
    }

    [Fact]
    public void Ctor_AutoCreateTableFalse_CreateTableSqlIsNull() {
        _output.WriteLine("[Scenario] Costruttore con autoCreateTable=false");
        _output.WriteLine("[Atteso] Nessun SQL di CREATE TABLE generato (_createTableSql == null)");

        var sql = Field(Create(autoCreate: false), "_createTableSql");

        _output.WriteLine($"[Restituito] _createTableSql={sql ?? "null"}");
        Assert.Null(sql);
    }

    [Fact]
    public void Ctor_AutoCreateTable_BuildsCreateTableWithTypesAndCharset() {
        _output.WriteLine("[Scenario] Costruttore con autoCreateTable=true e 2 colonne (a TEXT, b VARCHAR(32))");
        _output.WriteLine("[Atteso] CREATE TABLE IF NOT EXISTS `logs` con le colonne tipizzate NULL e charset utf8mb4_unicode_ci");

        var sql = Field(Create(), "_createTableSql")!;

        _output.WriteLine($"[Restituito] {sql}");
        Assert.Contains("CREATE TABLE IF NOT EXISTS `logs` (", sql);
        Assert.Contains("`a` TEXT NULL", sql);
        Assert.Contains("`b` VARCHAR(32) NULL", sql);
        Assert.Contains("utf8mb4_unicode_ci", sql);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void Ctor_AddAutoIncrementColumn_ControlsIdPrimaryKey(bool autoId) {
        _output.WriteLine($"[Scenario] Costruttore con autoCreateTable=true e addAutoIncrementColumn={autoId}");
        _output.WriteLine($"[Atteso] La colonna `Id` BIGINT AUTO_INCREMENT PRIMARY KEY è {(autoId ? "presente" : "assente")} nel CREATE TABLE");

        var sql = Field(Create(autoId: autoId), "_createTableSql")!;

        var hasId = sql.Contains("`Id` BIGINT NOT NULL AUTO_INCREMENT PRIMARY KEY");
        _output.WriteLine($"[Restituito] Colonna Id presente={hasId}");
        Assert.Equal(autoId, hasId);
    }

    [Fact]
    public void Ctor_DefaultColumns_InsertHasTenParameters() {
        _output.WriteLine("[Scenario] Costruttore con la mappa colonne di default (10 colonne)");
        _output.WriteLine("[Atteso] L'INSERT contiene @p0..@p9 e non contiene @p10");

        var sql = Field(Create(cols: MySqlColumnMap.Default()), "_insertSql")!;

        _output.WriteLine($"[Restituito] {sql}");
        Assert.Contains("@p9", sql);
        Assert.DoesNotContain("@p10", sql);
    }

    [Fact]
    public async Task EmitBatchAsync_EmptyBatch_ReturnsWithoutConnecting() {
        _output.WriteLine("[Scenario] EmitBatchAsync con batch vuoto su un sink con connection string irraggiungibile (Server=none)");
        _output.WriteLine("[Atteso] Nessun tentativo di connessione: SelfLog resta vuoto (il sink inghiotte gli errori di connessione nel SelfLog)");

        var selfLog = new StringWriter();
        Serilog.Debugging.SelfLog.Enable(selfLog);
        try {
            await Create().EmitBatchAsync([]);
        } finally {
            Serilog.Debugging.SelfLog.Disable();
        }

        _output.WriteLine($"[Restituito] SelfLog=\"{selfLog}\"");
        Assert.Equal(string.Empty, selfLog.ToString());
    }

    [Fact]
    public async Task OnEmptyBatchAsync_ReturnsCompletedTask() {
        _output.WriteLine("[Scenario] OnEmptyBatchAsync chiamato su un sink valido (hook invocato dal batching quando non ci sono eventi)");
        _output.WriteLine("[Atteso] Il Task si completa senza eccezioni");

        var ex = await Record.ExceptionAsync(() => Create().OnEmptyBatchAsync());

        _output.WriteLine($"[Restituito] Eccezione: {ex?.GetType().Name ?? "nessuna"}");
        Assert.Null(ex);
    }

    private static LogEvent Event(string template = "Hello {Name}", LogEventLevel level = LogEventLevel.Warning,
        Exception? ex = null, DateTimeOffset? ts = null, params (string Key, LogEventPropertyValue Value)[] props) =>
        new(ts ?? DateTimeOffset.UtcNow, level, ex, new MessageTemplateParser().Parse(template),
            props.Select(p => new LogEventProperty(p.Key, p.Value)));

    private static object? Extract(MySqlWriterKind kind, LogEvent evt, string? property = null, bool utc = true) =>
        Create(utc: utc).ExtractValue(new MySqlColumn("c", kind, "TEXT", property), evt);

    [Fact]
    public void ExtractValue_Rendered_ReturnsRenderedMessage() {
        _output.WriteLine("[Scenario] Writer Rendered su \"Hello {Name}\" con Name=42");
        _output.WriteLine("[Atteso] \"Hello 42\"");
        var v = Extract(MySqlWriterKind.Rendered, Event(props: ("Name", new ScalarValue(42))));
        _output.WriteLine($"[Restituito] {v}");
        Assert.Equal("Hello 42", v);
    }

    [Fact]
    public void ExtractValue_Template_ReturnsTemplateText() {
        _output.WriteLine("[Scenario] Writer Template");
        _output.WriteLine("[Atteso] Testo grezzo del template");
        Assert.Equal("Hello {Name}", Extract(MySqlWriterKind.Template, Event()));
    }

    [Fact]
    public void ExtractValue_Level_ReturnsLevelName() {
        _output.WriteLine("[Scenario] Writer Level con evento Warning");
        _output.WriteLine("[Atteso] \"Warning\"");
        Assert.Equal("Warning", Extract(MySqlWriterKind.Level, Event()));
    }

    [Fact]
    public void ExtractValue_Timestamp_Utc_ReturnsUtcDateTime() {
        _output.WriteLine("[Scenario] Writer Timestamp, storeTimestampInUtc=true, evento a +02:00");
        _output.WriteLine("[Atteso] DateTime Kind=Utc uguale a UtcDateTime");
        var ts = new DateTimeOffset(2026, 1, 2, 10, 0, 0, TimeSpan.FromHours(2));
        var v = Assert.IsType<DateTime>(Extract(MySqlWriterKind.Timestamp, Event(ts: ts), utc: true));
        _output.WriteLine($"[Restituito] {v:o} ({v.Kind})");
        Assert.Equal(DateTimeKind.Utc, v.Kind);
        Assert.Equal(ts.UtcDateTime, v);
    }

    [Fact]
    public void ExtractValue_Timestamp_Local_ReturnsLocalDateTime() {
        _output.WriteLine("[Scenario] Writer Timestamp, storeTimestampInUtc=false");
        _output.WriteLine("[Atteso] DateTime Kind=Local uguale a LocalDateTime (indipendente dal fuso della macchina)");
        var ts = new DateTimeOffset(2026, 1, 2, 10, 0, 0, TimeSpan.FromHours(2));
        var v = Assert.IsType<DateTime>(Extract(MySqlWriterKind.Timestamp, Event(ts: ts), utc: false));
        _output.WriteLine($"[Restituito] {v:o} ({v.Kind})");
        Assert.Equal(DateTimeKind.Local, v.Kind);
        Assert.Equal(ts.LocalDateTime, v);
    }

    [Fact]
    public void ExtractValue_Exception_NullWhenAbsent_TextWhenPresent() {
        _output.WriteLine("[Scenario] Writer Exception senza e con eccezione");
        _output.WriteLine("[Atteso] null senza eccezione; ToString() con eccezione");
        Assert.Null(Extract(MySqlWriterKind.Exception, Event()));
        var v = Assert.IsType<string>(Extract(MySqlWriterKind.Exception, Event(ex: new InvalidOperationException("boom"))));
        Assert.Contains("boom", v);
    }

    [Fact]
    public void ExtractValue_Serialized_ReturnsJsonWithRenderedMessage() {
        _output.WriteLine("[Scenario] Writer Serialized");
        _output.WriteLine("[Atteso] JSON valido con RenderedMessage");
        var v = Assert.IsType<string>(Extract(MySqlWriterKind.Serialized, Event(props: ("Name", new ScalarValue("x")))));
        using var doc = JsonDocument.Parse(v);
        Assert.Equal("Hello \"x\"", doc.RootElement.GetProperty("RenderedMessage").GetString());
    }

    [Fact]
    public void ExtractValue_Single_MissingProperty_ReturnsNull() {
        _output.WriteLine("[Scenario] Writer Single su proprieta' assente");
        _output.WriteLine("[Atteso] null");
        Assert.Null(Extract(MySqlWriterKind.Single, Event(), "Nope"));
    }

    [Fact]
    public void ExtractValue_Single_Scalars_AreUnquoted() {
        _output.WriteLine("[Scenario] Writer Single su scalar string e int");
        _output.WriteLine("[Atteso] \"abc\" senza apici; \"42\"");
        Assert.Equal("abc", Extract(MySqlWriterKind.Single, Event(props: ("P", new ScalarValue("abc"))), "P"));
        Assert.Equal("42", Extract(MySqlWriterKind.Single, Event(props: ("P", new ScalarValue(42))), "P"));
    }

    [Fact]
    public void ExtractValue_Single_NullScalar_FallsBackToValueToString() {
        _output.WriteLine("[Scenario] Writer Single su scalar null");
        _output.WriteLine("[Atteso] \"null\" (rendering Serilog)");
        Assert.Equal("null", Extract(MySqlWriterKind.Single, Event(props: ("P", new ScalarValue(null))), "P"));
    }

    [Fact]
    public void ExtractValue_Single_Structure_FallsBackToValueToString() {
        _output.WriteLine("[Scenario] Writer Single su structure");
        _output.WriteLine("[Atteso] Rendering Serilog della structure");
        var s = new StructureValue([new LogEventProperty("A", new ScalarValue(1))]);
        Assert.Equal(s.ToString(), Extract(MySqlWriterKind.Single, Event(props: ("P", s)), "P"));
    }

    [Fact]
    public void ExtractValue_Properties_SimplifiesAllValueKinds() {
        _output.WriteLine("[Scenario] Writer Properties con scalar, null, sequence, structure, dictionary");
        _output.WriteLine("[Atteso] JSON con valori nativi, array e oggetti");
        var dict = new DictionaryValue([new(new ScalarValue("k"), new ScalarValue(true))]);
        var v = Assert.IsType<string>(Extract(MySqlWriterKind.Properties, Event(props: [
            ("S", new ScalarValue(5)),
            ("N", new ScalarValue(null)),
            ("Q", new SequenceValue([new ScalarValue(1), new ScalarValue(2)])),
            ("O", new StructureValue([new LogEventProperty("A", new ScalarValue("x"))])),
            ("D", dict)])));
        _output.WriteLine($"[Restituito] {v}");
        var r = JsonDocument.Parse(v).RootElement;
        Assert.Equal(5, r.GetProperty("S").GetInt32());
        Assert.Equal(JsonValueKind.Null, r.GetProperty("N").ValueKind);
        Assert.Equal(2, r.GetProperty("Q").GetArrayLength());
        Assert.Equal("x", r.GetProperty("O").GetProperty("A").GetString());
        Assert.True(r.GetProperty("D").GetProperty("k").GetBoolean());
    }
}
