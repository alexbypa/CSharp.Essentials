using Microsoft.Extensions.DependencyInjection;

namespace CSharpEssentials.HttpHelper.HttpMocks;

public static class HttpMockInjection {
    public static IServiceCollection Inject(this IServiceCollection services) {
        services.AddScoped<IHttpMockScenario>((sp) => {
            return HttpMockScenarioLibrary.InternalErrorThenOk("api/keepalive");
        });
        services.AddScoped<IHttpMockScenario>((sp) => {
            return HttpMockScenarioLibrary.InternalErrorThenOk("api/certlogin");
        });
        services.AddScoped<IHttpMockScenario>((sp) => {
            return HttpMockScenarioLibrary.HostNotFound("eventtytpe");
        });
        services.AddScoped<HttpMockEngine>();
        return services;
    }
}