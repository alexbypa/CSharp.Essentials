dotnet run

Open **[http://localhost:5123](http://localhost:5123)**: the root opens the Playground (see below); every scenario is listed on `/swagger` and `/scalar`. Each endpoint produces structured logs visible immediately in the terminal and in the `Logs/` folder.

> In Development mode (`dotnet run` always uses Development), the `LoggerHelper` section of `appsettings.Development.json` configures **Console + File** only — no external services required.  
> To use external sinks (Email, SQL Server, PostgreSQL, Telegram, Elasticsearch, Seq, MySql), copy `appsettings.LoggerHelper.debug.example.json` to `appsettings.LoggerHelper.debug.json` (gitignored), fill in the placeholders, and it takes precedence. Set `ASPNETCORE_ENVIRONMENT=Production` to use `appsettings.LoggerHelper.json`.

Try `GET /api/httphelper/retry` to watch HttpHelper retries (two Warning attempts, then one Information) live in `/loggerhelper`. The mock's 502/503/200 cursor is shared by all callers, so the sequence only holds for serial requests: concurrent requests split it between them.

## Playground (all sinks, fluent API)

Open `/` (or `/playground.html`): pick sinks, minimum level per sink, message, properties and masking options, then **Send log**. Each request builds a new logger with the fluent API only (`AddRoute`, `ConfigureSeq`/`ConfigureFile`/`ConfigureElasticsearch`/`ConfigureMSSqlServer`/`ConfigurePostgreSql`/`ConfigureTelegram`, `EnableSensitiveDataMasking`), writes one event, disposes it (flush) and shows which sinks received it and the event after masking.

**Call API** runs HttpHelper against [httpbin.org](https://httpbin.org) (retries, timeout, rate limit, Bearer auth) and logs every attempt to the same sinks with HttpHelper's built-in events (including `CorrelationId`). The **E-commerce order** scenario runs login → order → payment against an in-memory shop (no network) with one `CorrelationId`, and the **HTTP log** checkbox turns the per-attempt events on and off. Telegram: `dotnet user-secrets set "Playground:Telegram:BotToken" "<token>"` and `"Playground:Telegram:ChatId"`. Full guide: [DEMO.md](../../DEMO.md).

External sinks run in Docker — see [`docker/README.md`](docker/README.md):

```bash
cd docker && cp .env.example .env && docker compose up -d
```

Connection strings are in the `Playground` section of `appsettings.json` (they match `.env.example`; change both if you change the passwords). API reference: `/scalar` (Scalar) or `/swagger`.
