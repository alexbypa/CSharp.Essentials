using CSharpEssentials.LoggerHelper;
using LoggerHelperDemo.Endpoints;
using LoggerHelperDemo.Persistence;
using LoggerHelperDemo.Repositories;
using LoggerHelperDemo.Services;
using Microsoft.EntityFrameworkCore;
using System;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddloggerConfiguration(builder);

// 1) DbContext PostgreSQL
builder.Services.AddDbContext<AppDbContext>(opt =>
    opt.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

// 2) Repository + Service + HttpClient
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddHttpClient<IUserService, UserService>();

builder.Services.AddOpenApi();
builder.Services.AddEndpointDefinitions();

// Configuro un HttpClient “tipizzato” per IUserService
builder.Services.AddHttpClient<IUserService, UserService>(client => {
    var cfg = builder.Configuration;
    client.BaseAddress = new Uri(cfg["Reqres:BaseUrl"]);
    client.DefaultRequestHeaders.Add("x-api-key", cfg["Reqres:ApiKey"]);
});

var app = builder.Build();

//app.UseMiddleware<RequestResponseLoggingMiddleware>();

app.UseEndpointDefinitions();

app.Run();
