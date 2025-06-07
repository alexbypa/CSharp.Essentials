using CSharpEssentials.LoggerHelper;
using LoggerHelperDemo.Entities;
using LoggerHelperDemo.Models;
using LoggerHelperDemo.Services;
using Microsoft.AspNetCore.Mvc;

namespace LoggerHelperDemo.Endpoints;

public class UserSyncEndpoint : IEndpointDefinition {
    public void DefineEndpoints(WebApplication app) {
        app.MapGet("/users/sync", SyncUsers)
           .WithName("SyncUsers")
           .Produces<List<User>>(StatusCodes.Status200OK);
    }
    // Single Responsibility: qui solo il mapping e il result
    private async Task<IResult> SyncUsers([FromQuery] int page, IUserService service) {
        loggerExtension<IRequest>.TraceAsync(new LoggerRequest(), Serilog.Events.LogEventLevel.Information, null, "Loaded LoggerHelper");
        if (!string.IsNullOrEmpty(loggerExtension<IRequest>.CurrentError))
            return Results.BadRequest(loggerExtension<IRequest>.CurrentError);
        if (loggerExtension<IRequest>._errors != null)
            return Results.BadRequest(string.Join(",", loggerExtension<IRequest>._errors.ToList().Select(item => $"{item.SinkName}: {item.ErrorMessage}")));
        var users = await service.SyncUsersAsync(page);
        return Results.Ok(users);
    }
}
