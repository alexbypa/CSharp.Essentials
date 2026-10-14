using Xunit.Abstractions;
using CSharpEssentials.LoggerHelper.Sink.MySql;

namespace CSharpEssentials.LoggerHelper.Tests;

public class MySqlColumnMapTests {
    private readonly ITestOutputHelper _output;

    public MySqlColumnMapTests(ITestOutputHelper output) {
        _output = output;
    }

    [Theory]
    [InlineData("a")]
    [InlineData("Log_2024")]
    public void EnsureValidIdentifier_ValidName_DoesNotThrow(string name) {
        _output.WriteLine($"[Scenario] Valido l'identificatore MySQL \"{name}\" (solo lettere, cifre e underscore)");
        _output.WriteLine("[Atteso] Nessuna eccezione: il nome è accettato");

        var ex = Record.Exception(() => MySqlColumnMap.EnsureValidIdentifier(name, "column name"));

        _output.WriteLine($"[Restituito] Eccezione: {ex?.GetType().Name ?? "nessuna"}");
        Assert.Null(ex);
    }

    [Fact]
    public void EnsureValidIdentifier_64Chars_DoesNotThrow() {
        _output.WriteLine("[Scenario] Valido un identificatore di esattamente 64 caratteri (limite massimo MySQL)");
        _output.WriteLine("[Atteso] Nessuna eccezione: 64 è ancora ammesso");

        var ex = Record.Exception(() => MySqlColumnMap.EnsureValidIdentifier(new string('a', 64), "column name"));

        _output.WriteLine($"[Restituito] Eccezione: {ex?.GetType().Name ?? "nessuna"}");
        Assert.Null(ex);
    }

    [Theory]
    [InlineData("a`b")]
    [InlineData("a;DROP TABLE x")]
    [InlineData("a b")]
    [InlineData("")]
    [InlineData("tab-le")]
    [InlineData("tàble")]
    [InlineData(null)]
    public void EnsureValidIdentifier_InvalidName_ThrowsInvalidOperationException(string? name) {
        _output.WriteLine($"[Scenario] Valido l'identificatore non sicuro/non valido \"{name ?? "null"}\" (backtick, SQL injection, spazi, vuoto, trattino, accenti o null)");
        _output.WriteLine("[Atteso] InvalidOperationException: il nome viene rifiutato");

        var ex = Record.Exception(() => MySqlColumnMap.EnsureValidIdentifier(name!, "column name"));

        _output.WriteLine($"[Restituito] Eccezione: {ex?.GetType().Name ?? "nessuna"}");
        Assert.IsType<InvalidOperationException>(ex);
    }

    [Fact]
    public void EnsureValidIdentifier_TrailingNewline_Throws() {
        _output.WriteLine("[Scenario] Valido \"abc\\n\": nome valido seguito da un a-capo finale (`$` in .NET lo accetterebbe)");
        _output.WriteLine("[Atteso] InvalidOperationException: l'ancora \\z impone la fine reale della stringa");

        var ex = Record.Exception(() => MySqlColumnMap.EnsureValidIdentifier("abc\n", "column name"));

        _output.WriteLine($"[Restituito] Eccezione: {ex?.GetType().Name ?? "nessuna"}");
        Assert.IsType<InvalidOperationException>(ex);
    }

    [Fact]
    public void EnsureValidIdentifier_65Chars_Throws() {
        _output.WriteLine("[Scenario] Valido un identificatore di 65 caratteri (un carattere oltre il limite MySQL)");
        _output.WriteLine("[Atteso] InvalidOperationException: nome troppo lungo");

        var ex = Record.Exception(() => MySqlColumnMap.EnsureValidIdentifier(new string('a', 65), "column name"));

        _output.WriteLine($"[Restituito] Eccezione: {ex?.GetType().Name ?? "nessuna"}");
        Assert.IsType<InvalidOperationException>(ex);
    }

    [Fact]
    public void EnsureValidIdentifier_Invalid_MessageContainsWhatAndValue() {
        _output.WriteLine("[Scenario] Valido \"a;b\" con etichetta \"table name\" e ispeziono il messaggio dell'eccezione");
        _output.WriteLine("[Atteso] Il messaggio contiene sia l'etichetta (\"table name\") sia il valore errato (\"a;b\")");

        var ex = Assert.Throws<InvalidOperationException>(() => MySqlColumnMap.EnsureValidIdentifier("a;b", "table name"));

        _output.WriteLine($"[Restituito] Message=\"{ex.Message}\"");
        Assert.Contains("table name", ex.Message);
        Assert.Contains("a;b", ex.Message);
    }

    [Fact]
    public void Quote_Identifier_WrapsInBackticks() {
        _output.WriteLine("[Scenario] Quote dell'identificatore \"logs\"");
        _output.WriteLine("[Atteso] \"`logs`\": l'identificatore è racchiuso tra backtick");

        var quoted = MySqlColumnMap.Quote("logs");

        _output.WriteLine($"[Restituito] {quoted}");
        Assert.Equal("`logs`", quoted);
    }

    [Fact]
    public void Default_ReturnsTenColumnsWithExpectedWritersAndTypes() {
        _output.WriteLine("[Scenario] Richiedo la mappa colonne di default (nessuna configurazione utente)");
        _output.WriteLine("[Atteso] 10 colonne; [0]=ApplicationName Single VARCHAR(255), [4]=raise_date Timestamp DATETIME(6), [6]=properties Serialized JSON");

        var cols = MySqlColumnMap.Default();

        _output.WriteLine($"[Restituito] Count={cols.Count}, [0]={cols[0]}, [4]={cols[4]}, [6]={cols[6]}");
        Assert.Equal(10, cols.Count);
        Assert.Equal(new MySqlColumn("ApplicationName", MySqlWriterKind.Single, "VARCHAR(255)", "ApplicationName"), cols[0]);
        Assert.Equal(new MySqlColumn("raise_date", MySqlWriterKind.Timestamp, "DATETIME(6)", null), cols[4]);
        Assert.Equal(new MySqlColumn("properties", MySqlWriterKind.Serialized, "JSON", null), cols[6]);
    }

    [Theory]
    [InlineData("a`b")]
    [InlineData("x;DROP TABLE y")]
    [InlineData("")]
    public void FromConfig_InvalidColumnName_ThrowsInvalidOperationException(string name) {
        _output.WriteLine($"[Scenario] FromConfig con una colonna dal nome non valido \"{name}\"");
        _output.WriteLine("[Atteso] InvalidOperationException: il nome colonna viene validato prima di costruire la mappa");

        var ex = Record.Exception(() => MySqlColumnMap.FromConfig([new() { Name = name }]));

        _output.WriteLine($"[Restituito] Eccezione: {ex?.GetType().Name ?? "nessuna"}");
        Assert.IsType<InvalidOperationException>(ex);
    }

    [Theory]
    [InlineData("foo")]
    [InlineData(null)]
    public void FromConfig_UnknownWriter_ThrowsInvalidOperationException(string? writer) {
        _output.WriteLine($"[Scenario] FromConfig con writer sconosciuto o mancante: \"{writer ?? "null"}\"");
        _output.WriteLine("[Atteso] InvalidOperationException: il writer non è tra quelli supportati");

        var ex = Record.Exception(() => MySqlColumnMap.FromConfig([new() { Name = "c", Writer = writer! }]));

        _output.WriteLine($"[Restituito] Eccezione: {ex?.GetType().Name ?? "nessuna"}");
        Assert.IsType<InvalidOperationException>(ex);
    }

    [Theory]
    [InlineData("RENDERED", "Rendered")]
    [InlineData("Template", "Template")]
    [InlineData("level", "Level")]
    [InlineData("TimeStamp", "Timestamp")]
    [InlineData("exception", "Exception")]
    [InlineData("Serialized", "Serialized")]
    [InlineData("PROPERTIES", "Properties")]
    [InlineData("single", "Single")]
    public void FromConfig_WriterIsCaseInsensitive_MapsKind(string writer, string expected) {
        _output.WriteLine($"[Scenario] FromConfig con writer \"{writer}\" scritto con maiuscole/minuscole arbitrarie");
        _output.WriteLine($"[Atteso] Il writer viene mappato (case-insensitive) a MySqlWriterKind.{expected}");

        var kind = MySqlColumnMap.FromConfig([new() { Name = "c", Writer = writer }])[0].Writer.ToString();

        _output.WriteLine($"[Restituito] {kind}");
        Assert.Equal(expected, kind);
    }

    [Fact]
    public void FromConfig_SingleWithoutProperty_UsesColumnName() {
        _output.WriteLine("[Scenario] Colonna \"c\" con writer Single e nessuna Property specificata");
        _output.WriteLine("[Atteso] Property = \"c\": ricade sul nome della colonna");

        var property = MySqlColumnMap.FromConfig([new() { Name = "c", Writer = "Single" }])[0].Property;

        _output.WriteLine($"[Restituito] Property={property ?? "null"}");
        Assert.Equal("c", property);
    }

    [Fact]
    public void FromConfig_SingleWithProperty_UsesProperty() {
        _output.WriteLine("[Scenario] Colonna \"c\" con writer Single e Property=\"P\"");
        _output.WriteLine("[Atteso] Property = \"P\": vince la property esplicita");

        var property = MySqlColumnMap.FromConfig([new() { Name = "c", Writer = "Single", Property = "P" }])[0].Property;

        _output.WriteLine($"[Restituito] Property={property ?? "null"}");
        Assert.Equal("P", property);
    }

    [Fact]
    public void FromConfig_NonSingleWriter_PropertyIsNull() {
        _output.WriteLine("[Scenario] Colonna con writer Level (non Single) ma con Property=\"P\" valorizzata");
        _output.WriteLine("[Atteso] Property = null: viene ignorata per i writer diversi da Single");

        var property = MySqlColumnMap.FromConfig([new() { Name = "c", Writer = "Level", Property = "P" }])[0].Property;

        _output.WriteLine($"[Restituito] Property={property ?? "null"}");
        Assert.Null(property);
    }

    [Theory]
    [InlineData("text", "TEXT")]
    [InlineData("LongText", "LONGTEXT")]
    [InlineData("json", "JSON")]
    [InlineData("datetime", "DATETIME(6)")]
    [InlineData("timestamp", "DATETIME(6)")]
    [InlineData("int", "INT")]
    [InlineData("bigint", "BIGINT")]
    [InlineData("bool", "TINYINT(1)")]
    [InlineData("boolean", "TINYINT(1)")]
    [InlineData("unknown", "TEXT")]
    [InlineData(null, "TEXT")]
    public void FromConfig_Type_MapsSqlType(string? type, string expected) {
        _output.WriteLine($"[Scenario] FromConfig con Type=\"{type ?? "null"}\"");
        _output.WriteLine($"[Atteso] SqlType = \"{expected}\" (tipi sconosciuti o assenti ricadono su TEXT)");

        var sqlType = MySqlColumnMap.FromConfig([new() { Name = "c", Type = type! }])[0].SqlType;

        _output.WriteLine($"[Restituito] SqlType={sqlType}");
        Assert.Equal(expected, sqlType);
    }

    [Theory]
    [InlineData(100, "VARCHAR(100)")]
    [InlineData(16383, "VARCHAR(16383)")]
    [InlineData(0, "VARCHAR(255)")]
    [InlineData(16384, "VARCHAR(255)")]
    [InlineData(-1, "VARCHAR(255)")]
    public void FromConfig_VarcharLength_ClampsOutOfRangeTo255(int length, string expected) {
        _output.WriteLine($"[Scenario] FromConfig con Type=VarChar e Length={length} (range valido 1..16383)");
        _output.WriteLine($"[Atteso] SqlType = \"{expected}\" (lunghezza valida usata così com'è, fuori range ricade su 255)");

        var sqlType = MySqlColumnMap.FromConfig([new() { Name = "c", Type = "VarChar", Length = length }])[0].SqlType;

        _output.WriteLine($"[Restituito] SqlType={sqlType}");
        Assert.Equal(expected, sqlType);
    }

    [Fact]
    public void FromConfig_EmptyList_ReturnsEmpty() {
        _output.WriteLine("[Scenario] FromConfig con lista di colonne vuota");
        _output.WriteLine("[Atteso] Mappa vuota (nessuna colonna, nessun default implicito)");

        var cols = MySqlColumnMap.FromConfig([]);

        _output.WriteLine($"[Restituito] Count={cols.Count}");
        Assert.Empty(cols);
    }
}
