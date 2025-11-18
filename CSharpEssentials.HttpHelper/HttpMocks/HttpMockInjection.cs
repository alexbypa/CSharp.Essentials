using Microsoft.Extensions.DependencyInjection;
using System.Net;

namespace CSharpEssentials.HttpHelper.HttpMocks;

public static class HttpMockInjection {
    public static IServiceCollection InjectMock(this IServiceCollection services) {
        services.AddTransient<IHttpMockEngine, HttpMockEngine>();
        //services.AddTransient<HttpMockDelegatingHandler>();

        return services;
    }
}