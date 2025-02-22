using CSharpEssentials.HttpHelper;
using CSharpEssentials.LoggerHelper;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

#region hangFire
// builder.Services.AddHangFire<Request>(builder);
#endregion
#region LoggerHelper
builder.Services.addloggerConfiguration(builder);
#endregion
#region httpExtension
builder.Services.AddHttpClients(builder.Configuration);
#endregion

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

#region hangFire
// app.UseCustomHangfireDashboard(builder.Configuration);
#endregion
// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment()) {
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseAuthorization();

app.MapControllers();

app.Run();
