using CSharpEssentials.LoggerHelper;
using CSharpEssentials.LoggerHelper.model;
using LoggerHelperDemo.Entities;
using LoggerHelperDemo.Models;
using LoggerHelperDemo.Services;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;

namespace LoggerHelperDemo.Endpoints;

public class UserSyncEndpoint : IEndpointDefinition {
    public void DefineEndpoints(WebApplication app) {
        app.MapGet("/users/sync", SyncUsers)
           .WithName("SyncUsers")
           .Produces<UsersDto>(StatusCodes.Status200OK);
    }
    // Single Responsibility: qui solo il mapping e il result
    private async Task<IResult> SyncUsers([FromQuery] int page, IUserService service) {
        loggerExtension<IRequest>.TraceSync(new LoggerRequest(), Serilog.Events.LogEventLevel.Information, null, "Loaded LoggerHelper");
        loggerExtension<IRequest>.TraceSync(new LoggerRequest(), Serilog.Events.LogEventLevel.Warning, null, "Write a warning example");
        loggerExtension<IRequest>.TraceSync(new LoggerRequest(), Serilog.Events.LogEventLevel.Error, null, "Write an error example");
        
        if (!string.IsNullOrEmpty(loggerExtension<IRequest>.CurrentError))
            return Results.BadRequest(loggerExtension<IRequest>.CurrentError);
        //if (loggerExtension<IRequest>.Errors.Any())
        //    return Results.BadRequest(string.Join(",", loggerExtension<IRequest>.Errors.ToList().Select(item => $"{item.SinkName}: {item.ErrorMessage}")));
        try {
            Debug.Print(loggerExtension<IRequest>.SinksLoaded.Count().ToString());
            var users = await service.SyncUsersAsync(page);
            var response = new UsersDto {
                Users = users.ToList(),
                Errors = loggerExtension<IRequest>.Errors.ToList()
            };
            return Results.Ok(response);
        } catch (Exception ex) {
            loggerExtension<IRequest>.TraceSync(new LoggerRequest(), Serilog.Events.LogEventLevel.Error, ex, "Error on call page {page} on reqres.in", page);
            return Results.BadRequest(ex.Message);
        }
    }

    public class UsersDto {
        public List<User> Users { get; set; }
        public List<LogErrorEntry> Errors { get; set; }
    }
}
