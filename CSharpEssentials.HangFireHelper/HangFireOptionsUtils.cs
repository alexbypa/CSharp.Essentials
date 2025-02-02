using Microsoft.Extensions.Configuration;

namespace CSharpEssentials.HangFireHelper;
public class HangFireOptionsUtils {
    public HangFireDashBoardAccessOptions[] getOperators() {
        IConfigurationBuilder builder = new ConfigurationBuilder();
        builder.AddJsonFile(Path.Combine(Directory.GetCurrentDirectory(), "appsettings.hangfire.json"));

        var root = builder.Build();
        var HangfireOptions = root.GetSection("HangfireOptions").Get<HangfireOptions>();

        string IpAuthorized = HangfireOptions.Dashboard.IpAuthorized;
        return HangfireOptions.Dashboard.AuthorizationHangFire
            .Select(item => new HangFireDashBoardAccessOptions {
                Login = item.UserName,
                PasswordClear = item.Password,
                isReadOnly = item.isReadOnly,
                IpAuthorized = item.IpAuthorized == null ? IpAuthorized : item.IpAuthorized
            }).ToArray();
    }
}
