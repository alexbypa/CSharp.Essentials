namespace LoggerHelperDemo.Endpoints;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

public static class EndpointDefinitionExtensions {
    public static IServiceCollection AddEndpointDefinitions(this IServiceCollection services) {
        // ogni volta che crei un nuovo endpoint, aggiungi qui una riga
        services.AddSingleton<IEndpointDefinition, UserSyncEndpoint>();
        // services.AddSingleton<IEndpointDefinition, AnotherEndpoint>();
        return services;
    }

    public static WebApplication UseEndpointDefinitions(this WebApplication app) {
        var defs = app.Services.GetServices<IEndpointDefinition>();
        foreach (var def in defs)
            def.DefineEndpoints(app);
        return app;
    }
}
