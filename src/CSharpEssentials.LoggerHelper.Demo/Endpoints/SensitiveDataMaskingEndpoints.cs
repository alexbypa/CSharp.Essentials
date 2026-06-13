namespace CSharpEssentials.LoggerHelper.Demo.Endpoints;

/// <summary>
/// Esempio 7: Sensitive data masking — redazione automatica di PII e segreti.
/// Dimostra l'enricher built-in che oscura email, carte di credito, token Bearer/JWT,
/// password nelle connection string e proprietà strutturate sensibili — configurato
/// interamente via JSON in "LoggerHelper:SensitiveDataMasking", senza toccare i call site.
/// </summary>
public class SensitiveDataMaskingEndpoints : IEndpointDefinition {
    public void DefineEndpoints(WebApplication app) {
        var group = app.MapGroup("/api/masking").WithTags("Sensitive Data Masking");

        // GET /api/masking/login — email + password in proprietà strutturate
        group.MapGet("/login", (ILogger<SensitiveDataMaskingEndpoints> logger) => {
            logger.LogInformation(
                "Login attempt for {Email} with {Password} from card {CardNumber}",
                "alice@example.com", "Sup3rSecret!", "4532-1234-5678-9012");

            return Results.Ok(new {
                message = "Check Console/Logs — Email, Password and CardNumber are masked before reaching any sink"
            });
        })
        .WithSummary("Mask email, password and credit card in structured properties")
        .WithDescription(
            "Logs a single event with three structured placeholders: {Email}, {Password}, {CardNumber}. " +
            "With 'LoggerHelper:SensitiveDataMasking:Enabled' = true and the 'Email' / 'CreditCard' presets active, " +
            "plus 'Password' listed under SensitiveProperties, the values are redacted to '***MASKED***' " +
            "for every configured sink (Console, File, SQL, Elasticsearch...) — no per-sink configuration needed.");

        // GET /api/masking/api-error — Bearer token + connection string in messaggio
        group.MapGet("/api-error", (ILogger<SensitiveDataMaskingEndpoints> logger) => {
            logger.LogError(
                "Downstream call failed. Authorization: Bearer eyJhbGciOiJIUzI1NiJ9.eyJzdWIiOiIxMjM0NTY3ODkwIn0.abc123, " +
                "ConnectionString: Server=db;User Id=sa;Password=S3cr3t!;, OrderId: ORD-99821");

            return Results.Ok(new {
                message = "Check Console/Logs — Bearer token, connection-string password and ORD-* id are masked, " +
                           "while OrderId text and surrounding context remain readable"
            });
        })
        .WithSummary("Mask a Bearer token, a connection-string password and a custom regex rule")
        .WithDescription(
            "Logs a single literal message containing a Bearer token, a SQL connection string with an embedded " +
            "password, and an order id. With 'EnableRenderedMessage' + 'SensitiveDataMasking' both enabled, the " +
            "'BearerToken' and 'ConnectionStringSecret' presets mask only the secret portion (keeping 'Bearer ' " +
            "and 'Password=' visible for context), and the custom rule 'ORD-\\\\d+' masks the order id — " +
            "demonstrating that built-in presets and user-defined regex rules compose in the same pipeline.");
    }
}
