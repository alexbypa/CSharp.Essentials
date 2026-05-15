using Microsoft.Extensions.Configuration;

namespace CSharpEssentials.LoggerHelper.Tests;

public class LegacyConfigurationAdapterTests {
    [Fact]
    public void TryApply_MapsSerilogConfiguration_ToRoutes() {
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

        Assert.True(applied);
        Assert.Equal("LegacyApp", options.ApplicationName);
        Assert.Equal(2, options.Routes.Count);
        Assert.Contains(options.Routes, r => r.Sink == "Console");
        Assert.NotNull(options.RawSinksSection);
    }
}
