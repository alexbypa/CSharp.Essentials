namespace CSharpEssentials.LoggerHelper.Demo.Endpoints;

public interface IEndpointDefinition {
    void DefineEndpoints(WebApplication app);
}

public static class EndpointDefinitionExtensions {
    public static WebApplication UseEndpointDefinitions(this WebApplication app) {
        var defs = app.Services.GetServices<IEndpointDefinition>();
        foreach (var def in defs)
            def.DefineEndpoints(app);
        return app;
    }
}
