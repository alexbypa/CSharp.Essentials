# CSharpEssentials.LoggerHelper.Sink.Telegram

> Telegram bot notifications with MarkdownV2 formatting and throttling for [CSharpEssentials.LoggerHelper](https://www.nuget.org/packages/CSharpEssentials.LoggerHelper).

Part of the **CSharpEssentials.LoggerHelper** ecosystem — install only the sinks you need.

---

## Install

```bash
dotnet add package CSharpEssentials.LoggerHelper
dotnet add package CSharpEssentials.LoggerHelper.Sink.Telegram
```

---

## Quick Setup — JSON

```json
{
  "LoggerHelper": {
    "ApplicationName": "MyApp",
    "Routes": [
      { "Sink": "Telegram", "Levels": ["Error", "Fatal"] }
    ],
    "Sinks": {
      "Telegram": {
        "BotToken": "123456:ABC-DEF...",
        "ChatId": "-100123456789"
      }
    }
  }
}
```

```csharp
builder.Services.AddLoggerHelper(builder.Configuration);
```

## Quick Setup — Fluent API

```csharp
builder.Services.AddLoggerHelper(b => b
    .WithApplicationName("MyApp")
    .AddRoute("Telegram", LogEventLevel.Error, LogEventLevel.Fatal)
    .ConfigureTelegram(t => {
        t.BotToken = "123456:ABC-DEF...";
        t.ChatId = "-100123456789";
    })
);
```

---

## Configuration Options

| Property | Type | Default | Description |
|----------|------|---------|-------------|
| `BotToken` | `string` | `""` | Telegram Bot API token |
| `ChatId` | `string` | `""` | Target chat/group/channel ID |
| `ThrottleInterval` | `TimeSpan?` | 1 second | Minimum interval between messages |

Messages are formatted with **MarkdownV2** and include emoji indicators per level:
- Information: `INFO`
- Warning: `WARNING`
- Error / Fatal: `ERROR` / `FATAL`

---

## Links

- [Documentation](https://www.loggerhelper.it)
- [CSharpEssentials.LoggerHelper (core)](https://www.nuget.org/packages/CSharpEssentials.LoggerHelper)
- [GitHub Repository](https://github.com/alexbypa/CSharp.Essentials)
- [MIT License](https://github.com/alexbypa/CSharp.Essentials/blob/main/LICENSE)
