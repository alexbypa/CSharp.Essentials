using Microsoft.Extensions.DependencyInjection;

namespace CSharpEssentials.LoggerHelper.AI.Infrastructure {
    public static class AiServiceExtensions {
        public static IServiceCollection AddAiServices(this IServiceCollection services) {

            services.AddControllers();
            return services;
        }
    }
}
