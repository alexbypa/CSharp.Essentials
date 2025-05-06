using CSharpEssentials.LoggerHelper;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

// Aggiungi il logger di sistema
builder.Services.AddLogging(logging => {
    logging.ClearProviders();
    logging.AddConsole();
});

var app = builder.Build();

// Middleware per loggare tutte le richieste HTTP
app.UseMiddleware<RequestResponseLoggingMiddleware>();

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

app.Run();