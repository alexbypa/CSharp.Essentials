using Microsoft.Extensions.Configuration;

namespace CSharpEssentials.LoggerHelper;

/// <summary>
/// Maps legacy Serilog:SerilogConfiguration JSON to v5 LoggerHelperOptions.
/// </summary>
internal static class LegacyConfigurationAdapter {
    internal static bool TryApply(IConfiguration configuration, LoggerHelperOptions options) {
        var legacyRoot = configuration.GetSection("Serilog:SerilogConfiguration");
        if (!legacyRoot.Exists())
            return false;

        var appName = legacyRoot["ApplicationName"];
        if (!string.IsNullOrWhiteSpace(appName))
            options.ApplicationName = appName;

        foreach (var condition in legacyRoot.GetSection("SerilogCondition").GetChildren()) {
            var sink = condition["Sink"];
            if (string.IsNullOrWhiteSpace(sink))
                continue;

            var levels = condition.GetSection("Level").Get<string[]>() ?? [];
            options.Routes.Add(new SinkRouting {
                Sink = sink,
                Levels = levels.ToList()
            });
        }

        var serilogOption = legacyRoot.GetSection("SerilogOption");
        if (serilogOption.Exists())
            options.RawSinksSection = LegacySinksSectionFactory.Create(serilogOption);

        var enableSelf = legacyRoot["SerilogOption:GeneralConfig:EnableSelfLogging"];
        if (bool.TryParse(enableSelf, out var selfLog))
            options.General.EnableSelfLogging = selfLog;

        return options.Routes.Count > 0;
    }
}

/// <summary>
/// Builds a v5 "Sinks" configuration section from legacy SerilogOption.
/// </summary>
internal static class LegacySinksSectionFactory {
    internal static IConfigurationSection Create(IConfigurationSection serilogOption) {
        var sinks = new Dictionary<string, Dictionary<string, string?>>(StringComparer.OrdinalIgnoreCase);

        AddSink(sinks, serilogOption, "File", "File");
        AddSink(sinks, serilogOption, "Email", "Email");
        AddSink(sinks, serilogOption, "MSSqlServer", "MSSqlServer");
        AddSink(sinks, serilogOption, "PostgreSQL", "PostgreSql");
        AddSink(sinks, serilogOption, "ElasticSearch", "Elasticsearch");
        AddSink(sinks, serilogOption, "SeqOptions", "Seq");
        AddTelegram(sinks, serilogOption.GetSection("TelegramOption"));

        var data = new Dictionary<string, object?> { ["Sinks"] = sinks };
        return new ConfigurationBuilder()
            .AddInMemoryCollection(Flatten(data))
            .Build()
            .GetSection("Sinks");
    }

    private static void AddSink(
        Dictionary<string, Dictionary<string, string?>> sinks,
        IConfigurationSection parent,
        string legacyKey,
        string v5Key) {
        var section = parent.GetSection(legacyKey);
        if (!section.Exists())
            return;

        var map = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
        foreach (var kv in section.AsEnumerable(makePathsRelative: true)) {
            if (kv.Value is null || string.IsNullOrEmpty(kv.Key))
                continue;
            var key = kv.Key.Contains(':') ? kv.Key.Split(':')[^1] : kv.Key;
            map[key] = kv.Value;
        }

        sinks[v5Key] = map;
    }

    private static void AddTelegram(
        Dictionary<string, Dictionary<string, string?>> sinks,
        IConfigurationSection telegram) {
        if (!telegram.Exists())
            return;

        sinks["Telegram"] = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase) {
            ["BotToken"] = telegram["Api_Key"] ?? telegram["BotToken"],
            ["ChatId"] = telegram["chatId"] ?? telegram["ChatId"],
            ["ThrottleInterval"] = telegram["ThrottleInterval"]
        };
    }

    private static IEnumerable<KeyValuePair<string, string?>> Flatten(Dictionary<string, object?> data, string prefix = "") {
        foreach (var (key, value) in data) {
            var path = string.IsNullOrEmpty(prefix) ? key : $"{prefix}:{key}";
            switch (value) {
                case Dictionary<string, Dictionary<string, string?>> nestedSinks:
                    foreach (var (sinkName, props) in nestedSinks)
                        foreach (var (prop, propVal) in props)
                            yield return new KeyValuePair<string, string?>($"{path}:{sinkName}:{prop}", propVal);
                    break;
                default:
                    yield return new KeyValuePair<string, string?>(path, value?.ToString());
                    break;
            }
        }
    }
}
