using Hangfire;
using Hangfire.Console;
using Hangfire.PostgreSql;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace CSharpEssentials.HangFireHelper;
public static class HangFireExtension {
    public static IServiceCollection AddHangFire<IRequest>(this IServiceCollection services, WebApplicationBuilder builder) {
        var externalConfigPath = Path.Combine(Directory.GetCurrentDirectory(), "appsettings.hangfire.json");
        if (File.Exists(externalConfigPath)) {
            builder.Configuration.AddJsonFile(externalConfigPath, optional: true, reloadOnChange: true);
        }

        HangfireOptions hangfireOptions = builder.Configuration.GetSection("HangfireOptions").Get<HangfireOptions>();
        services.Configure<HangfireOptions>(builder.Configuration);
        string connectionstring = hangfireOptions.ConnectionString;

        services.AddHttpClient<HangFireHttpJobRequestHandler>();

        services.AddHangfire(configuration =>
            configuration.UsePostgreSqlStorage(connectionstring, new PostgreSqlStorageOptions {
                DistributedLockTimeout = TimeSpan.FromSeconds(30)
            })
            .UseConsole()
            //configuration.UseSqlServerStorage(connectionstring)
            .UseRecommendedSerializerSettings()
            //.UseSerilogLogProvider() //TODO: aggiungere dipendenza serilog
            .UseSerializerSettings(new JsonSerializerSettings {
                ReferenceLoopHandling = ReferenceLoopHandling.Ignore, //TODO: usare System.Text.Json al posto di newtonsoft
                ContractResolver = new DefaultContractResolver {
                    NamingStrategy = new CamelCaseNamingStrategy()
                }
            })
        );
        services.AddHangfireServer();

        services.AddTransient<IHangFireJobRequestHandler<HangFireHttpJobRequest>, HangFireHttpJobRequestHandler>();
        services.AddTransient<BackgroundJobHandler>();
        return services;
    }
}

