using CSharpEssentials.LoggerHelper.Dashboard;
using System.Net;
using System.Net.Http.Headers;
using System.Text;

namespace CSharpEssentials.LoggerHelper.Tests;

public class DashboardBasicAuthenticationHandlerTests {
    private const string User = "admin";
    private const string Pass = "s3cret";

    public static TheoryData<string> RouteData => new(DashboardTestHost.Routes);

    private static Task<DashboardTestHost> StartAsync() =>
        DashboardTestHost.StartAsync(o => o.UseBasicAuthentication(User, Pass));

    private static Action<HttpRequestMessage> Basic(string user, string pass) =>
        r => r.Headers.Authorization = new AuthenticationHeaderValue("Basic",
            Convert.ToBase64String(Encoding.UTF8.GetBytes($"{user}:{pass}")));

    [Theory]
    [InlineData(null, "p")]
    [InlineData("", "p")]
    [InlineData(" ", "p")]
    [InlineData("u", null)]
    [InlineData("u", "")]
    [InlineData("u", " ")]
    public void UseBasicAuthentication_InvalidCredentials_ThrowsArgumentException(string? user, string? pass) {
        Assert.ThrowsAny<ArgumentException>(() => new DashboardOptions().UseBasicAuthentication(user, pass));
    }

    [Theory]
    [MemberData(nameof(RouteData))]
    public async Task Route_NoCredentials_Returns401WithBasicChallenge(string route) {
        await using var host = await StartAsync();
        using var response = await host.GetAsync(route);
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        var challenge = Assert.Single(response.Headers.WwwAuthenticate);
        Assert.Equal("Basic", challenge.Scheme);
        Assert.Contains("LoggerHelper Dashboard", challenge.Parameter);
    }

    [Theory]
    [InlineData("wrong user")]
    [InlineData("wrong pass")]
    [InlineData("invalid base64")]
    [InlineData("no colon")]
    [InlineData("bearer")]
    public async Task Route_BadCredentials_Returns401(string kind) {
        await using var host = await StartAsync();
        Action<HttpRequestMessage> configure = kind switch {
            "wrong user" => Basic("other", Pass),
            "wrong pass" => Basic(User, "nope"),
            "invalid base64" => r => r.Headers.TryAddWithoutValidation("Authorization", "Basic !!!not-base64!!!"),
            "no colon" => r => r.Headers.Authorization = new AuthenticationHeaderValue("Basic", Convert.ToBase64String(Encoding.UTF8.GetBytes("adminsecret"))),
            _ => r => r.Headers.Authorization = new AuthenticationHeaderValue("Bearer", "token")
        };
        Assert.Equal(HttpStatusCode.Unauthorized, await host.GetStatusAsync("/loggerhelper/api/status", configure));
    }

    [Theory]
    [MemberData(nameof(RouteData))]
    public async Task Route_ValidCredentials_Returns200(string route) {
        await using var host = await StartAsync();
        Assert.Equal(HttpStatusCode.OK, await host.GetStatusAsync(route, Basic(User, Pass)));
    }

    [Theory]
    [InlineData("/loggerhelper/")]
    [InlineData("/LoggerHelper/API/Status")]
    public async Task Route_PathVariantsNoCredentials_Returns401(string route) {
        await using var host = await StartAsync();
        Assert.Equal(HttpStatusCode.Unauthorized, await host.GetStatusAsync(route));
    }

    [Fact]
    public async Task Route_PasswordContainingColon_Returns200() {
        await using var host = await DashboardTestHost.StartAsync(o => o.UseBasicAuthentication(User, "pa:ss:word"));
        Assert.Equal(HttpStatusCode.OK, await host.GetStatusAsync("/loggerhelper/api/status", Basic(User, "pa:ss:word")));
    }

    [Fact]
    public async Task Route_BasicWithAppDefaultScheme_OnlyDefaultSchemeUser_Returns401() {
        await using var host = await StartAsync();
        Assert.Equal(HttpStatusCode.Unauthorized,
            await host.GetStatusAsync("/loggerhelper/api/status", r => r.Headers.Add("X-Test-User", "alice")));
    }

    [Fact]
    public async Task Route_BasicWithPolicy_ValidBasicButPolicyFails_Returns403() {
        await using var host = await DashboardTestHost.StartAsync(
            o => { o.UseBasicAuthentication(User, Pass); o.AuthorizationPolicy = "Admins"; },
            a => a.AddPolicy("Admins", p => p.RequireRole("admin")));
        Assert.Equal(HttpStatusCode.Forbidden, await host.GetStatusAsync("/loggerhelper/api/status", Basic(User, Pass)));
    }
}
