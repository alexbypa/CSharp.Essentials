using CSharpEssentials.LoggerHelper;
using CSharpEssentials.LoggerHelper.TestApp;
using Serilog.Events;

var builder = WebApplication.CreateBuilder(args);

// ══════════════════════════════════════════════════════════════════
// OPZIONE A — config da JSON (appsettings.LoggerHelper.[Debug.]json)
// In Development carica appsettings.LoggerHelper.Debug.json
// In produzione carica appsettings.LoggerHelper.json
// ══════════════════════════════════════════════════════════════════
builder.Services.AddLoggerHelper(builder.Configuration);

// Aggiunta Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new() 
    { 
        Title = "LoggerHelper Test API", 
        Version = "v1", 
        Description = "Minimal API Swagger ben stilizzato per i casi d'uso di LoggerHelper" 
    });
});

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI(c => 
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "LoggerHelper Test API v1");
    // Opzionale: route prefissa vuota per aprire swagger sulla root, per test e comodità
    c.RoutePrefix = string.Empty;
});

app.UseLoggerHelper();

// Usa la classe per mappare tutti i casi d'uso
app.MapLoggerEndpoints();

app.Run();

// Required for WebApplicationFactory<Program> in integration tests
public partial class Program { }
