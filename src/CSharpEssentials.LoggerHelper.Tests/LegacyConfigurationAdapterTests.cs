using Xunit.Abstractions;
using Microsoft.Extensions.Configuration;

namespace CSharpEssentials.LoggerHelper.Tests;

public class LegacyConfigurationAdapterTests {
    private readonly ITestOutputHelper _output;

    public LegacyConfigurationAdapterTests(ITestOutputHelper output) {
        _output = output;
    }

    [Fact]
    public void TryApply_MapsSerilogConfiguration_ToRoutes() {
        _output.WriteLine("[Scenario] JSON legacy v2-v4 (Serilog:SerilogConfiguration con ApplicationName, 2 SerilogCondition Console/Email e SerilogOption Email/Telegram) passato a LegacyConfigurationAdapter.TryApply");
        _output.WriteLine("[Atteso] TryApply == true; ApplicationName=\"LegacyApp\"; 2 Routes (incluso Console); RawSinksSection valorizzata");

        var config = new ConfigurationBuilder()
            .AddJsonStream(new MemoryStream("""
            {
              "Serilog": {
                "SerilogConfiguration": {
                  "ApplicationName": "LegacyApp",
                  "SerilogCondition": [
                    { "Sink": "Console", "Level": ["Information"] },
                    { "Sink": "Email", "Level": ["Error"] }
                  ],
                  "SerilogOption": {
                    "Email": {
                      "Host": "smtp.test.com",
                      "Port": 587,
                      "From": "a@b.com",
                      "To": "ops@test.com"
                    },
                    "TelegramOption": {
                      "Api_Key": "token",
                      "chatId": "12345",
                      "ThrottleInterval": "00:00:30"
                    }
                  }
                }
              }
            }
            """u8.ToArray()))
            .Build();

        var options = new LoggerHelperOptions();
        var applied = LegacyConfigurationAdapter.TryApply(config, options);

        _output.WriteLine($"[Restituito] applied={applied}, ApplicationName={options.ApplicationName}, Routes=[{string.Join(", ", options.Routes.Select(r => r.Sink))}], RawSinksSection={(options.RawSinksSection is null ? "null" : "presente")}");
        Assert.True(applied);
        Assert.Equal("LegacyApp", options.ApplicationName);
        Assert.Equal(2, options.Routes.Count);
        Assert.Contains(options.Routes, r => r.Sink == "Console");
        Assert.NotNull(options.RawSinksSection);
    }
}
