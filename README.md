# TJProcessor

A .NET 8 integration platform that mediates between a local PostgreSQL database and the Tajikistan national product-marking authority (`pub-api.mark.tj`). Used by manufacturers to register batches, mint marking codes, aggregate cases/SSCCs, and submit reports.

## Stack

- ASP.NET Core 8 minimal-host API (`TJConnector.Api`)
- Blazor Server UI (`TJConnector.Web`)
- PostgreSQL 15 via EF Core (`TJConnector.Postgres`)
- MassTransit on an in-memory bus — no external broker; consumers run in-process inside the API
- Optional read-only Microsoft SQL Server for legacy container queries (`TJConnector.StateSystem`)
- Dapper for the SQL-Server read paths

## Layout

```
TJConnector.Api/            ASP.NET Core API + MassTransit consumers
  Transit/                  package/SSCC aggregation pipeline (10 consumers)
  TransitBatches/           batch orchestration
  TestRun/                  test-run choreography
  Migrations/               EF Core migrations
TJConnector.Web/            Blazor Server UI
TJConnector.Postgres/       EF Core DbContext + entity configurations
TJConnector.StateSystem/    Dapper queries against external MS SQL state DB
TJConnector.Shared/         shared DTOs and contracts
docs/                       project documentation
docker-compose.yml          API + Web + Postgres
```

## Build and run

```bash
docker compose up --build
```

The API listens on `http://localhost:5166` (Swagger at `/swagger` in development). The Web UI binds in the same compose file. PostgreSQL data volume is named `tjprocessor-pgdata`.

For local development without Docker:

```bash
dotnet build TJConnector.sln
dotnet run --project TJConnector.Api
dotnet run --project TJConnector.Web
```

## Configuration

- API URL templates and feature flags live in `TJConnector.Api/appsettings.json`.
- The external MS SQL connection string (read-only state DB) is in the same file under `StateSystem`. Leave blank to disable.
- Local dev overrides go in `appsettings.Development.json` (not committed).

## Database

Schema is owned by EF Core (`TJConnector.Postgres.ApplicationDbContext`). Migrations live in `TJConnector.Api/Migrations/`. New env: run `dotnet ef database update --project TJConnector.Api`. Demo seed: `psql -f docs/sql/seed-demo-data.sql`.
