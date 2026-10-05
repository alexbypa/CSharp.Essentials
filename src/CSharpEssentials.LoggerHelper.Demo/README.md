dotnet run

Open **[http://localhost:5000/swagger](http://localhost:5000/swagger)** — the Swagger UI lists every available scenario. Each endpoint produces structured logs visible immediately in the terminal and in the `Logs/` folder.

> In Development mode (`dotnet run` always uses Development), the `LoggerHelper` section of `appsettings.Development.json` configures **Console + File** only — no external services required.  
> To use external sinks (Email, SQL Server, PostgreSQL, Telegram, Elasticsearch, Seq, MySql), copy `appsettings.LoggerHelper.debug.example.json` to `appsettings.LoggerHelper.debug.json` (gitignored), fill in the placeholders, and it takes precedence. Set `ASPNETCORE_ENVIRONMENT=Production` to use `appsettings.LoggerHelper.json`.

Try `GET /api/httphelper/retry` to watch HttpHelper retries (two Warning attempts, then one Information) live in `/loggerhelper`.
