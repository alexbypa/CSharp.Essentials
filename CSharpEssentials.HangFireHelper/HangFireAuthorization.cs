using CSharpEssentials.EncryptHelper;
using CSharpEssentials.HttpContextHelper;
using Hangfire.Dashboard;
using System.Text;

namespace CSharpEssentials.HangFireHelper;
public class HangFireAuthorization : IDashboardAuthorizationFilter {
    private readonly BasicAuthAuthorizationFilterOptions _options;

    public HangFireAuthorization(BasicAuthAuthorizationFilterOptions options) {
        _options = options;
    }
    public bool Authorize(DashboardContext context) {
        var httpContext = context.GetHttpContext();
        if (!httpContext.Request.Headers.ContainsKey("Authorization")) {
            httpContext.Response.Headers["WWW-Authenticate"] = "Basic realm=\"Hangfire Dashboard\"";
            httpContext.Response.StatusCode = 401;
            return false;
        }
        var authHeader = httpContext.Request.Headers["Authorization"].ToString();
        var authHeaderVal = System.Net.Http.Headers.AuthenticationHeaderValue.Parse(authHeader);

        if (authHeaderVal.Scheme.Equals("basic", StringComparison.OrdinalIgnoreCase) &&
            !string.IsNullOrWhiteSpace(authHeaderVal.Parameter)) {
            var credentials = Encoding.UTF8
                .GetString(Convert.FromBase64String(authHeaderVal.Parameter))
                .Split(':', 2);

            var username = credentials[0];
            var password = credentials[1].toMD5();

            // Verifica le credenziali
            var userToVerify = _options.Users.Where(user => user.Login == username && user.PasswordClear == password).FirstOrDefault();
            if (userToVerify != null) {

                //httpContext.Session.SetString("userLogged", userToVerify.Login);

                bool IsIpAllowed = false;
                SessionCatcher session = new SessionCatcher(httpContext);
                IsIpAllowed = session.IsIpAddressValid(session.ipAddress, userToVerify.IpAuthorized);
                if (!IsIpAllowed) {
                    //TODO:
                    //var request = new Request { Action = ActionRequest.verifyTokenAuthentication, IdTransaction = "*" };
                    //loggerExtension.TraceSync(request, Serilog.Events.LogEventLevel.Error, null, $"Autenticazione non valida per utente {username} Ip di Accesso : {session.ipAddress}");
                    httpContext.Response.Headers.Add("HangFire-DENY-IP", $"notAuthorized.html?Message=IP {session.ipAddress} non autorizzato");
                    return false;
                }
            }

            return userToVerify != null;
        }
        return false;
    }
}
public class BasicAuthAuthorizationFilterOptions {
    public HangFireDashBoardAccessOptions[] Users { get; set; }
}
