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

## Get a bot token and your chat id (3 minutes)

1. **Create the bot**: in Telegram, open [@BotFather](https://t.me/BotFather), send `/newbot`, pick a name and a username ending in `bot`. BotFather replies with the **bot token** (`123456789:AAH...`). Keep it secret: whoever has it controls the bot.
2. **Write to the bot first**: open the chat with your bot and press **Start** (or send `/start`). A bot cannot message someone who never wrote to it.
3. **Read your chat id**: open `https://api.telegram.org/bot<TOKEN>/getUpdates` in the browser and look for `"chat":{"id":123456789,...}`. That number is the **ChatId**.
   - Empty `"result":[]`? Send another message to the bot and reload.
   - For a group: add the bot to the group, write a message there, reload. Group ids are negative (`-100...`).

Keep the token out of source control, e.g. with user-secrets in development:

```bash
dotnet user-secrets set "LoggerHelper:Sinks:Telegram:BotToken" "123456789:AAH..."
dotnet user-secrets set "LoggerHelper:Sinks:Telegram:ChatId" "123456789"
```

User-secrets apply when you call `AddLoggerHelper(builder.Configuration)` and no `appsettings.LoggerHelper.json` (or `appsettings.LoggerHelper.debug.json` in Development) sits in the working directory. If one of those files exists, it replaces `builder.Configuration`: set the environment variables `LoggerHelper__Sinks__Telegram__BotToken` and `LoggerHelper__Sinks__Telegram__ChatId` instead.

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
| `BotToken` | `string` | `""` | **Required.** Telegram Bot API token |
| `ChatId` | `string` | `""` | **Required.** Target chat/group/channel ID |
| `ThrottleInterval` | `TimeSpan?` | 1 second | Minimum interval between messages |

> **Required:** `BotToken` and `ChatId`. If the `Telegram` section is missing or one of them is empty, the sink fails to configure (`InvalidOperationException`), LoggerHelper records it as not configured (FAILED in the Dashboard/MCP) and the other sinks keep working.

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
