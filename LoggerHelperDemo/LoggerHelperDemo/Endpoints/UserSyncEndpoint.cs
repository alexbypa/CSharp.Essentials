using LoggerHelperDemo.Entities;
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
        var users = await service.SyncUsersAsync(page);
        return Results.Ok(users);
    }
}
