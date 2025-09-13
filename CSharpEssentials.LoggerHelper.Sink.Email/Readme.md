# ✉️ CSharpEssentials.LoggerHelper.Sink.Email

[![NuGet](https://img.shields.io/nuget/v/CSharpEssentials.LoggerHelper.Sink.Email.svg)](https://www.nuget.org/packages/CSharpEssentials.LoggerHelper.Sink.Email)
A flexible **HTML Email sink** for [CSharpEssentials.LoggerHelper](https://github.com/alexbypa/CSharp.Essentials/tree/main/CSharpEssentials.LoggerHelper.Sink.Email), designed for **real-time critical alerts** with full customization.

---

## 🔥 Key Features

* 📧 Send **real-time email alerts** for `Error` and `Fatal` events.
* 🎨 Full **HTML template customization** (dynamic placeholders, colors, log levels).
* ⏱️ Built-in **throttling** to prevent email floods.
* 🔒 Secure **SMTP with SSL/TLS**.
* ⚡ Drop-in integration with LoggerHelper’s level-based routing.

---

## 📦 Installation

```bash
dotnet add package CSharpEssentials.LoggerHelper.Sink.Email
```

---

## ⚡ Quick Configuration

```json
"SerilogOption": {
  "Email": {
    "From": "alerts@example.com",
    "Host": "smtp.gmail.com",
    "Port": 587,
    "To": "ops-team@example.com",
    "Username": "your-username",
    "Password": "your-password",
    "EnableSsl": true,
    "TemplatePath": "Templates/email-template-default.html",
    "ThrottleInterval": "00:01:00"
  }
}
```

---

## 🚀 Demo Project

Try it live with the [**CSharpEssentials.Extensions Demo**](https://github.com/alexbypa/Csharp.Essentials.Extensions) – a ready-to-run project showcasing the Email sink together with other sinks (File, SQL, Telegram, Telemetry).

---

## 🏷️ Tags

```

