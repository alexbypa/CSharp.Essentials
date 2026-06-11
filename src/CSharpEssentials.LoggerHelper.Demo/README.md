dotnet run

Open **[http://localhost:5000/swagger](http://localhost:5000/swagger)** — the Swagger UI lists every available scenario. Each endpoint produces structured logs visible immediately in the terminal and in the `Logs/` folder.

> In Development mode (`dotnet run` always uses Development), the project automatically reads `appsettings.LoggerHelper.debug.json`, which configures only **Console + File** — no SQL Server or PostgreSQL required.  
> To enable all 4 sinks (Console, File, MSSqlServer, PostgreSQL), set `ASPNETCORE_ENVIRONMENT=Production`.
