using CSharpEssentials.LoggerHelper;
using CSharpEssentials.HttpHelper;
using Microsoft.OpenApi.Models;
using Serilog.Events;
var builder = WebApplication.CreateBuilder(args);


// Add services to the container.

#region hangFire
// builder.Services.AddHangFire<Request>(builder);
#endregion
#region LoggerHelper
builder.Services.AddloggerConfiguration(builder);
#endregion
#region httpExtension
builder.Services.AddOptions();
builder.Services.AddHttpClients(builder.Configuration);
#endregion
builder.Services.AddHttpClient();

builder.Services.AddControllers();
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
app.UseStaticFiles(); // << deve essere PRIMA di app.UseRouting()

#region hangFire
// app.UseCustomHangfireDashboard(builder.Configuration);
#endregion
// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment()) {
    app.UseSwagger();
    app.UseSwaggerUI();
}
#region loggerExtension
app.UseMiddleware<RequestResponseLoggingMiddleware>();
#endregion
app.UseAuthorization();

app.MapControllers();

app.Run();
