using Hangfire.Dashboard;
using Hangfire;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;

namespace CSharpEssentials.HangFireHelper;
public static class HangfireMiddlewareExtensions {
    public static IApplicationBuilder UseCustomHangfireDashboard(this IApplicationBuilder app, IConfiguration configuration) {
        HangfireOptions hangfireOptions = configuration.GetSection("HangfireOptions").Get<HangfireOptions>();
        var dashboardOptions = new DashboardOptions {
            IsReadOnlyFunc = (DashboardContext context) => false,
            Authorization = new[]{
                new HangFireAuthorization(new BasicAuthAuthorizationFilterOptions
                {
                    Users = new HangFireOptionsUtils().getOperators()
                })
            }, IgnoreAntiforgeryToken = true
        };
        app.UseHangfireDashboard(hangfireOptions.Dashboard.RelativePath, dashboardOptions);
        return app;
    }
}