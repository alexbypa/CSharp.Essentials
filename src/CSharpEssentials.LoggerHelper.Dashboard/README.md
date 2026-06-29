# CSharpEssentials.LoggerHelper.Dashboard

Embedded real-time diagnostics dashboard for LoggerHelper — no external tools needed.

## Quick Start

```bash
dotnet add package CSharpEssentials.LoggerHelper.Dashboard
```

```csharp
// Program.cs
builder.Services.AddLoggerHelper(builder.Configuration);
builder.Services.AddLoggerHelperDashboard();

var app = builder.Build();
app.UseLoggerHelper();
app.MapLoggerHelperDashboard();  // → /loggerhelper

app.Run();
```

Navigate to `https://localhost:5001/loggerhelper` to see your dashboard.

## Features

- **Sink Health Cards** — See which sinks are active/failed at a glance
- **Live Log Stream** — Tail your logs in the browser via Server-Sent Events
- **Error History** — Click-to-expand recent sink errors with stack traces
- **Context Before Error** — When an Error/Fatal triggers a context flush, a dedicated panel shows all ring buffer entries that preceded the crash (timestamp, level, source, message) with level-appropriate coloring
- **Routing Config** — Visual map of which levels go where
- **Runtime Controls** — Toggle sinks and change log levels without restart
- **Dark Theme** — Easy on the eyes during late-night debugging
- **Zero Dependencies** — Pure HTML/CSS/JS, no npm, no bundler
- **Mobile Friendly** — Responsive layout works on any screen

## Configuration

```csharp
builder.Services.AddLoggerHelperDashboard(options => {
    options.RequireAuthorization = true;  // protect with ASP.NET auth
});
```

## License

MIT — [loggerhelper.com](https://www.loggerhelper.com)