using CSharpEssentials.LoggerHelper.Dashboard;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using System.Net;

namespace CSharpEssentials.LoggerHelper.Tests;

public class DashboardExtensionsTests {
    public static TheoryData<string> RouteData => new(DashboardTestHost.Routes);

    private static void AuthenticatedUser(HttpRequestMessage r) => r.Headers.Add("X-Test-User", "alice");

    [Fact]
    public void MapLoggerHelperDashboard_NoAuthentication_Throws() {
        var app = DashboardTestHost.Build(null, map: false);
        var ex = Assert.Throws<InvalidOperationException>(() => app.MapLoggerHelperDashboard());
        Assert.Contains("requires authentication", ex.Message);
    }

    [Fact]
    public void MapLoggerHelperDashboard_AddDashboardNotCalled_Throws() {
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();
        var app = builder.Build();
        var ex = Assert.Throws<InvalidOperationException>(() => app.MapLoggerHelperDashboard());
        Assert.Contains("AddLoggerHelperDashboard", ex.Message);
    }

    [Fact]
    public void MapLoggerHelperDashboard_MultipleSchemesNoDefault_Throws() {
        var app = DashboardTestHost.Build(null, s => s.AddAuthentication()
            .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>("A", null)
            .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>("B", null), map: false);
        Assert.Throws<InvalidOperationException>(() => app.MapLoggerHelperDashboard());
    }

    [Fact]
    public void MapLoggerHelperDashboard_UnknownAuthorizationPolicy_Throws() {
        var app = DashboardTestHost.Build(o => o.AuthorizationPolicy = "Nope", map: false);
        var ex = Assert.Throws<InvalidOperationException>(() => app.MapLoggerHelperDashboard());
        Assert.Contains("Nope", ex.Message);
    }

    [Fact]
    public void MapLoggerHelperDashboard_BasicWithUnknownPolicy_Throws() {
        var app = DashboardTestHost.Build(o => { o.UseBasicAuthentication("u", "p"); o.AuthorizationPolicy = "Nope"; }, map: false);
        var ex = Assert.Throws<InvalidOperationException>(() => app.MapLoggerHelperDashboard());
        Assert.Contains("Nope", ex.Message);
    }

    [Theory]
    [MemberData(nameof(RouteData))]
    public async Task Route_Anonymous_Returns401(string route) {
        await using var host = await DashboardTestHost.StartAsync(null);
        Assert.Equal(HttpStatusCode.Unauthorized, await host.GetStatusAsync(route));
    }

    [Theory]
    [MemberData(nameof(RouteData))]
    public async Task Route_Authenticated_Returns200(string route) {
        await using var host = await DashboardTestHost.StartAsync(null);
        Assert.Equal(HttpStatusCode.OK, await host.GetStatusAsync(route, AuthenticatedUser));
    }

    [Theory]
    [InlineData("/loggerhelper/")]
    [InlineData("/LoggerHelper/API/Status")]
    public async Task Route_AnonymousPathVariants_Returns401(string route) {
        await using var host = await DashboardTestHost.StartAsync(null);
        Assert.Equal(HttpStatusCode.Unauthorized, await host.GetStatusAsync(route));
    }

    [Theory]
    [InlineData("", "/")]
    [InlineData("", "/api/status")]
    [InlineData("/", "/")]
    [InlineData("/", "/api/status")]
    public async Task Route_RootPath_Anonymous_Returns401(string path, string route) {
        await using var host = await DashboardTestHost.StartAsync(o => o.Path = path);
        Assert.Equal(HttpStatusCode.Unauthorized, await host.GetStatusAsync(route));
    }

    [Fact]
    public async Task Route_CustomPathWithTrailingSlash_IsProtected() {
        await using var host = await DashboardTestHost.StartAsync(o => o.Path = "/ops/dash/");
        Assert.Equal(HttpStatusCode.Unauthorized, await host.GetStatusAsync("/ops/dash/api/status"));
        Assert.Equal(HttpStatusCode.OK, await host.GetStatusAsync("/ops/dash/api/status", AuthenticatedUser));
    }

    [Fact]
    public async Task Route_UserFailingPolicy_Returns403() {
        await using var host = await DashboardTestHost.StartAsync(
            o => o.AuthorizationPolicy = "Admins",
            a => a.AddPolicy("Admins", p => p.RequireRole("admin")));
        Assert.Equal(HttpStatusCode.Forbidden, await host.GetStatusAsync("/loggerhelper/api/status", AuthenticatedUser));
    }

    [Fact]
    public async Task Route_UserSatisfyingPolicy_Returns200() {
        await using var host = await DashboardTestHost.StartAsync(
            o => o.AuthorizationPolicy = "Admins",
            a => a.AddPolicy("Admins", p => p.RequireRole("admin")));
        Assert.Equal(HttpStatusCode.OK, await host.GetStatusAsync("/loggerhelper/api/status", r => {
            AuthenticatedUser(r);
            r.Headers.Add("X-Test-Role", "admin");
        }));
    }
}
