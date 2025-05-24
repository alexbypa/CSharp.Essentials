using CSharpEssentials.LoggerHelper;
using CSharpEssentials.LoggerHelper.Telemetry.Controllers;
using Microsoft.OpenApi.Models;
using Serilog.Events;
using Test.Controllers.logger;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddHttpClient();

#region LoggerHelper
builder.Services.AddSingleton<IContextLogEnricher, MyCustomEnricher>();
builder.Services.AddloggerConfiguration(builder);
#endregion

builder.Services
    .AddControllers();

// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c => {
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "LoggerHelper Test", Version = "v1" });

    c.MapType<LogEventLevel>(() => new OpenApiSchema {
        Type = "string",
        Enum = Enum.GetNames(typeof(LogEventLevel))
            .Select(name => new Microsoft.OpenApi.Any.OpenApiString(name))
            .Cast<Microsoft.OpenApi.Any.IOpenApiAny>()
            .ToList()
    });
});

var app = builder.Build();
LoggerHelperServiceLocator.Instance = app.Services;

app.UseStaticFiles(); // << deve essere PRIMA di app.UseRouting()

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment()) {
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseAuthorization();

app.MapControllers();


app.Run();
