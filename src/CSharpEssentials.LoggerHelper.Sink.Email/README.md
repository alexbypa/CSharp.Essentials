# CSharpEssentials.LoggerHelper.Sink.Email

> SMTP email alerts with HTML templates and throttling for [CSharpEssentials.LoggerHelper](https://www.nuget.org/packages/CSharpEssentials.LoggerHelper).

Part of the **CSharpEssentials.LoggerHelper** ecosystem — install only the sinks you need.

---

## Install

```bash
dotnet add package CSharpEssentials.LoggerHelper
dotnet add package CSharpEssentials.LoggerHelper.Sink.Email
```

---

## Quick Setup — JSON

```json
{
  "LoggerHelper": {
    "ApplicationName": "MyApp",
    "Routes": [
      { "Sink": "Email", "Levels": ["Error", "Fatal"] }
    ],
    "Sinks": {
      "Email": {
        "From": "alerts@myapp.com",
        "To": "team@myapp.com",
        "Host": "smtp.gmail.com",
        "Port": 587,
        "Username": "alerts@myapp.com",
        "Password": "app-password",
        "EnableSsl": true
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
    .AddRoute("Email", LogEventLevel.Error, LogEventLevel.Fatal)
    .ConfigureEmail(e => {
        e.From = "alerts@myapp.com";
        e.To = "team@myapp.com";
        e.Host = "smtp.gmail.com";
        e.Port = 587;
        e.Username = "alerts@myapp.com";
        e.Password = "app-password";
    })
);
```

---

## Configuration Options

| Property | Type | Default | Description |
|----------|------|---------|-------------|
| `From` | `string` | `""` | Sender email address |
| `To` | `string` | `""` | Recipient email address (comma-separated for multiple) |
| `Host` | `string` | `""` | SMTP server hostname |
| `Port` | `int` | `587` | SMTP server port |
| `Username` | `string?` | `null` | SMTP authentication username |
| `Password` | `string?` | `null` | SMTP authentication password |
| `EnableSsl` | `bool` | `true` | Enable SSL/TLS for SMTP connection |
| `TemplatePath` | `string?` | `null` | Path to a custom HTML email template |
| `ThrottleInterval` | `TimeSpan?` | `null` | Minimum interval between emails to prevent flooding |

The sink includes a built-in HTML template with color-coded log levels. Provide your own template via `TemplatePath` using placeholders like `{{Timestamp}}`, `{{Level}}`, `{{Message}}`, `{{ApplicationName}}`.

---

## Links

- [Documentation](https://www.loggerhelper.com)
- [CSharpEssentials.LoggerHelper (core)](https://www.nuget.org/packages/CSharpEssentials.LoggerHelper)
- [GitHub Repository](https://github.com/alexbypa/CSharp.Essentials)
- [MIT License](https://github.com/alexbypa/CSharp.Essentials/blob/main/LICENSE)
