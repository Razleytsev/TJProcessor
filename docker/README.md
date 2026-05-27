# Docker stack

Spin up Postgres + API + Web in one command and open the UI in your browser.

## Requirements

- Docker Desktop 4.x / Docker Engine 24+ with Compose v2.
- ~3 GB free disk (most of it is the .NET 8 SDK base image used at build time).
- `TJConnector.Api/appsettings.json` present locally (it is gitignored).
  - If you don't have one yet, copy [`appsettings.api.example.json`](appsettings.api.example.json) to `TJConnector.Api/appsettings.json` and set `TJConnection.Token` to the value provided by mark.tj.
  - The `ConnectionStrings.LocalDb` value in the JSON is **overridden** at runtime by an env var that points at the `postgres` service inside the compose network — you can leave it as `localhost` for local non-Docker runs.

## Run

```bash
docker compose up --build
```

First run takes a few minutes (downloads the SDK + runtime images, restores NuGet, builds two projects). Subsequent runs are seconds when only sources change.

Once you see:

```
tj-api  | Now listening on: http://[::]:5166
tj-web  | Now listening on: http://[::]:5113
```

open **http://localhost:5113** in your browser. Swagger is at **http://localhost:5166/swagger**.

## What's included

| Service  | Image / build                    | Port (host) | Notes |
|----------|----------------------------------|-------------|-------|
| postgres | `postgres:15-alpine`             | **55432**   | Persisted in named volume `tj-postgres-data`. Mapped to 55432 on the host (clashes too often otherwise; Docker Desktop on Windows pre-reserves ports near 5432). Inside the compose network the API still reaches it on 5432. |
| api      | built from `TJConnector.Api/Dockerfile` | 5166 | Schema auto-created on first run via `EnsureCreated()` |
| web      | built from `TJConnector.Web/Dockerfile` | 5113 | Blazor Server; talks to api at `http://api:5166` over the compose network |

## What's NOT included

- **SQL Server** (the `ExternalDb` connection) is optional and only used by container content/info queries. Those code paths will fail at call time if you exercise them; the UI itself loads end-to-end without it.
- **Real marking-authority access**. The external API is `https://pub-api.mark.tj:5230` and your container must have outbound HTTPS for live integration to work. The UI loads either way.

## Common operations

```bash
# Stop the stack but keep the database
docker compose down

# Stop the stack AND wipe the database (clean slate)
docker compose down --volumes

# Tail logs from one service
docker compose logs -f api
docker compose logs -f web

# Rebuild after a code change
docker compose up --build api web
```

## Troubleshooting

| Symptom | Likely cause |
|---|---|
| `web` container exits with `connection refused` from `http://api:5166` | API isn't healthy yet. Compose v2 doesn't gate `web` on api-healthy by default; just retry after a moment, or add a healthcheck for the api. |
| API exits with `password authentication failed` | The `ConnectionStrings__LocalDb` env var in `docker-compose.yml` and the postgres `POSTGRES_PASSWORD` are out of sync. Defaults are `postgres / postgres`. |
| `mount: ./TJConnector.Api/appsettings.json` errors | The file doesn't exist locally yet. Copy `docker/appsettings.api.example.json` into place. |
| UI loads but pages stay on "Loading…" | The API can't reach Postgres or its external dependency. Check `docker compose logs api`. |
