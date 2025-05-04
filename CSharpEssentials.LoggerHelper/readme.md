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

The library requires an external configuration file named `appsettings.LoggerHelper.json` in your project root directory. The library will read its configuration exclusively from this file.

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
        },
        {
          "Sink": "ElasticSearch",
          "Level": []
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

## Log Level Configuration

The library uses the `SerilogCondition` array in the configuration to determine which log levels should be written to each sink:

- Each sink has its own configuration entry with an array of log levels
- If a level is present in the array, logs of that level will be written to the corresponding sink
- If the `Level` array for a sink is empty (like `"Level": []`), **no logs** will be written to that sink regardless of their level
- If a sink is not listed in the `SerilogCondition` array, it won't be used

For example, in the configuration above:
- The Console sink will receive Information, Warning, Error, and Fatal logs
- The PostgreSQL and MSSqlServer sinks will only receive Error and Fatal logs
- The Telegram sink will only receive Fatal logs
- The ElasticSearch sink won't receive any logs (empty array)

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

// Add this middleware to automatically log all requests and responses with Information level
app.UseMiddleware<RequestResponseLoggingMiddleware>();

app.Run();
```

> **Note**: By adding `app.UseMiddleware<RequestResponseLoggingMiddleware>()` to your Program.cs, all HTTP requests and responses in your Web API will be automatically logged with Information level.

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