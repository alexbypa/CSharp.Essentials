# Demo Playground — Docker infrastructure

Infrastructure for the Demo Playground (`/playground.html`): every database/server sink in Docker.

Host ports 11433 and 15432 avoid clashes with a local SQL Server/PostgreSQL. Use `127.0.0.1` rather than `localhost`: on Windows `localhost` may try IPv6 first and add seconds to every connection.

## 1. Start the infrastructure

```bash
cp .env.example .env          # then change the passwords
docker compose up -d          # SQL Server, PostgreSQL, Seq, Elasticsearch
docker compose --profile kibana up -d   # optional: also Kibana
docker compose ps             # wait until every service is "healthy"
```

| Service | Address | Connection |
|---|---|---|
| SQL Server | `127.0.0.1:11433` | `Server=127.0.0.1,11433;Database=LoggerHelperPlayground;User Id=sa;Password=<MSSQL_SA_PASSWORD>;TrustServerCertificate=true` |
| PostgreSQL | `127.0.0.1:15432` | `Host=127.0.0.1;Port=15432;Database=loggerhelper_playground;Username=postgres;Password=<POSTGRES_PASSWORD>` |
| Seq | http://127.0.0.1:5341 | UI and ingestion on the same URL, no login |
| Elasticsearch | http://127.0.0.1:9200 | security disabled |
| Kibana | http://localhost:5601 | only with `--profile kibana` |

Databases are created on first start; log tables are created by the sinks.

Stop: `docker compose down` · Stop and wipe data: `docker compose down -v`
