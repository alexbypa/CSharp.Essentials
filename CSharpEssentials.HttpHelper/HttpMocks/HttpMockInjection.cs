using Microsoft.Extensions.DependencyInjection;

namespace CSharpEssentials.HttpHelper.HttpMocks;

public static class HttpMockInjection {
    public static IServiceCollection InjectMock(this IServiceCollection services) {
        //TODO: Questi servizi esternamente !
        services.AddScoped<IHttpMockScenario>((sp) => {
            return HttpMockScenarioLibrary.InternalErrorThenOk("api/keepalive");
        });
        services.AddScoped<IHttpMockScenario>((sp) => {
            return HttpMockScenarioLibrary.InternalErrorThenOk("api/certlogin");
        });
        services.AddScoped<IHttpMockScenario>((sp) => {
            return HttpMockScenarioLibrary.HostNotFound("eventtytpe");
        });
        services.AddScoped<IHttpMockScenario>((sp) => {
            return HttpMockScenarioLibrary.OkWithCustomBody("httpbin.org");
        });
        services.AddScoped<HttpMockEngine>();
        return services;
    }
}