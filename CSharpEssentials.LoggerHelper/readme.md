# CSharpEssentials.LoggerHelper

A flexible and powerful logging library for .NET applications that simplifies the implementation of structured logging with multiple sink options.

## Features

- Multi-sink logging support:
  - SQL Server
  - PostgreSQL
  - Console
  - File
  - Elasticsearch
  - Email
  - Telegram
- Conditional logging based on log levels
- Structured logging with custom properties
- Asynchronous and synchronous logging methods
- Machine name, transaction ID, and action tracking
- JSON configuration support

## Installation

```bash
dotnet add package CSharpEssentials.LoggerHelper
```

## Configuration

Create an `appsettings.LoggerHelper.json` file in your project root with the following structure:

```json
{
  "Serilog": {
    "SerilogConfiguration": {
      "SerilogCondition": [
        {
          "Sink": "Console",
          "Level": ["Information", "Warning", "Error", "Fatal"]
        },
        {
          "Sink": "File",
          "Level": ["Error", "Fatal"]
        },
        {
          "Sink": "PostgreSQL",
          "Level": ["Error", "Fatal"]
        },
        {
          "Sink": "MSSqlServer",
          "Level": ["Error", "Fatal"]
        },
        {
          "Sink": "Telegram",
          "Level": ["Fatal"]
        }
      ],
      "SerilogOption": {
        "PostgreSQL": {
          "connectionstring": "Host=localhost;Database=logs;Username=postgres;Password=yourpassword"
        },
        "MSSqlServer": {
          "connectionString": "Server=localhost;Database=Logs;Trusted_Connection=True;",
          "sinkOptionsSection": {
            "tableName": "Logs",
            "schemaName": "dbo",
            "autoCreateSqlTable": true,
            "batchPostingLimit": 50,
            "period": "00:00:30"
          }
        },
        "TelegramOption": {
          "Api_Key": "your-telegram-bot-api-key",
          "chatId": "your-chat-id"
        },
        "File": {
          "Path": "logs"
        },
        "GeneralConfig": {
          "EnableSelfLogging": false
        }
      }
    }
  }
}
```

## Setup in ASP.NET Core Application

Register the logger in your `Program.cs`:

```csharp
using CSharpEssentials.LoggerHelper;

var builder = WebApplication.CreateBuilder(args);

// Add logger configuration
builder.Services.addloggerConfiguration(builder);

// ... other services

var app = builder.Build();

// ... configure app

app.Run();
```

## Usage

First, implement the `IRequest` interface in your request model:

```csharp
public class MyRequest : IRequest
{
    public string IdTransaction { get; set; } = Guid.NewGuid().ToString();
    public string Action { get; set; } = "YourActionName";
    
    // Other properties
}
```

Then use the logger in your code:

```csharp
using CSharpEssentials.LoggerHelper;
using Serilog.Events;

// For synchronous logging
var request = new MyRequest();
loggerExtension<MyRequest>.TraceSync(
    request,
    LogEventLevel.Information,
    null,
    "Operation completed successfully: {OperationName}",
    "CreateUser"
);

// For asynchronous logging
await Task.Run(() => loggerExtension<MyRequest>.TraceAsync(
    request,
    LogEventLevel.Error,
    exception,
    "Error during operation: {OperationName}",
    "UpdateUser"
));

// For logging without a request object
loggerExtension<IRequest>.TraceSync(
    null,
    LogEventLevel.Warning,
    null,
    "System warning: {WarningMessage}",
    "Low disk space"
);
```

## Log Parameters

The `TraceSync` and `TraceAsync` methods accept the following parameters:

- **request**: An object implementing `IRequest` (contains IdTransaction and Action)
- **level**: Log severity level (Debug, Information, Warning, Error, Fatal)
- **ex**: Exception object (can be null)
- **message**: Log message with optional placeholders for variables
- **args**: Additional parameters to be inserted into message placeholders

## Database Schema

### PostgreSQL

The logger creates a table with the following columns:
- timestamp
- level
- message
- exception
- properties
- IdTransaction
- MachineName
- Action

### SQL Server

The logger creates a table with the following columns:
- LogEvent (JSON)
- IdTransaction
- MachineName
- Action

## License

[MIT](LICENSE)

## Author

Alessandro Chiodo

## Version

Current version: 1.0.1