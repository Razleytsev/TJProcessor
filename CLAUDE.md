# CLAUDE.md — TJConnector / TJProcessor

## PROJECT OVERVIEW

**TJConnector** is a .NET 8 integration platform for managing the lifecycle of **product marking codes** (Tajikistan national marking system). It acts as a bridge between a local PostgreSQL database and the external `pub-api.mark.tj` marking authority API. The system handles code emission requests, batch processing, container aggregation, and status tracking.

---

## SOLUTION STRUCTURE

```
TJProcessor/
├── TJConnector.sln
├── TJConnector.Api/            ← REST API backend (ASP.NET Core 8)
├── TJConnector.Postgres/       ← EF Core DbContext + entity configs
├── TJConnector.StateSystem/    ← External API client + SQL Server legacy access
├── TJConnector.Web/            ← Blazor Server frontend
└── SharedLibrary/              ← Shared DTOs and forms
```

---

## TECH STACK

| Layer | Technology | Notes |
|-------|-----------|-------|
| API Framework | ASP.NET Core 8 | Minimal hosting model |
| ORM | Entity Framework Core 9.0.1 | Async everywhere |
| Primary DB | PostgreSQL 15 | via Npgsql 9.0.3 |
| Legacy DB | SQL Server | via Dapper 2.1.35 |
| Message Bus | MassTransit 8.3.6 | In-memory transport |
| HTTP Resilience | Polly 8.5.1 | Retry + timeout + circuit breaker |
| Logging | Serilog 9.0.0 | Console + daily rolling file |
| JSON | Newtonsoft.Json 13.0.3 | EF Core JSONB columns |
| API Docs | Swashbuckle 6.6.2 | Swagger UI in dev |
| Frontend | Blazor Server 8.0 | Server-side rendering |

---

## PROJECT DETAILS

### TJConnector.Api
- **Port**: `5166` (API), `5167` (HTTPS)
- **Entry**: `Program.cs` — registers all services, EF Core, MassTransit, Serilog
- **Controllers**: `OrderController` at `/api/order`
- **Services**: `OrderService` (core business logic)
- **MassTransit consumers** (in-memory bus):
  - `StateCheckSSCC`, `ExternalDbCheck`, `ExternalDbContent`
  - `StateCreateApplication`, `StateApplicationStatus`, `StateProcessApplication`
  - `StateCreateAggregation`, `StateAggregationStatus`, `StateProcessAggregation`
  - `ReprocessConsumer`, `BatchInitialConsumer`, `CreateOrdersConsumer`, `ProcessOrdersConsumer`
- **Database init**: `dbContext.Database.EnsureCreated()` on startup (dev mode)
- **Migrations assembly**: `TJConnector.Api`
- **SignalR hub** (commented out): `/orderhub` — infrastructure exists, not active

### TJConnector.Postgres
- **DbContext**: `ApplicationDbContext`
- **Entity configs**: Each entity has `IEntityTypeConfiguration<T>` in separate files
- **Key pattern**: JSONB columns for status history arrays via `HasConversion` + Newtonsoft.Json
- **Timestamp default**: `RecordDate` uses `HasDefaultValueSql("NOW()")`

### TJConnector.StateSystem
- **External API**: `https://pub-api.mark.tj:5230` (Basic Auth via Base64 token)
- **Services**:
  - `ExternalEmissionService` — marking code emission (create/process/download)
  - `ExternalContainerService` — container aggregation operations
  - `ExternalProductService` — product catalog from external system
  - `ExternalDbData` — SQL Server queries via Dapper
- **SQL files** (gitignored — contain sensitive queries):
  - `ContainerContentQuery.sql`
  - `ContainerInfoQuery.sql`
- **Resilience** (`CustomHttpClient`):
  - Retry: 3 attempts, exponential backoff (2^n seconds: 2, 4, 8)
  - Timeout: 15 seconds per request
  - Circuit breaker: opens after 3 failures, breaks for 60 seconds

### TJConnector.Web (Blazor Server)
- **Default route**: `/` → redirects to `/batches`
- **Pages**:
  - `Batches.razor` — main order/batch management (split-pane: list + detail)
  - `Order.razor` — individual order detail view
  - `Products.razor` — product listing
  - `Containers.razor` — container management
  - `CustomTable.razor` — shared reusable table component
- **Services injected**: `IBatchServiceWeb`, `IProductService`, `IMetadataService`, `IOrderServiceWeb`, `IPackageRequestService`
- **Backend URL**: `http://localhost:5166` (configured in appsettings)
- **SignalR**: Hub configured with 512KB max message size

### SharedLibrary
- `OrderCreateForm` — DTO for creating orders (ProductUid, FactoryUid, MarkingLineUid, Count, Type)

---

## DATABASE SCHEMA (PostgreSQL)

### `CodeOrders`
| Column | Type | Notes |
|--------|------|-------|
| Id | int PK | Auto-increment |
| Type | int | Code type: 0–3 (3 = container/aggregation) |
| ProductId | int FK | → Products |
| Count | int | Requested code count |
| Status | int | See status table below |
| ExternalGuid | UUID | Reference in external marking system |
| Description | varchar(100) | |
| User | varchar(20) | |
| RecordDate | timestamp | Default: NOW() |
| StatusHistoryJson | JSONB | Array of `{Status, StatusDate}` |
| StatusMessage | text | Error/info text |

### `Status Codes`
| Value | Meaning |
|-------|---------|
| -4 | Unknown external status |
| -3 | Cancelled |
| -2 | Not approved |
| -1 | Creation failed |
| 0 | Created (pending) |
| 1 | Emission initiated |
| 2 | Approved |
| 3 | Processing |
| 4 | Ready for download |
| 5 | Completed |

### `Products`
| Column | Type | Notes |
|--------|------|-------|
| Id | int PK | |
| Type | int | Product type category |
| Gtin | varchar(20) | Global Trade Item Number |
| Name | varchar(200) | |
| ExternalUid | UUID | Reference in external system |
| RecordDate | timestamp | Default: NOW() |

### Other Tables
- **Factories**: Id, Name, ExternalUid
- **Locations**: Id, Name, ExternalUid
- **MarkingLines**: Id, Name, ExternalUid
- **Packages**: Code, SsccCode, Status, Content (JSONB)
- **PackageRequests**: Filename, User, Status, StatusHistoryJson
- **Batches**: Groups multiple CodeOrders — similar structure to CodeOrder
- **CodeOrderContent**: Downloaded code content, download history

---

## API ENDPOINTS

### `OrderController` — `/api/order`

| Method | Route | Description |
|--------|-------|-------------|
| GET | `/` | List all orders |
| GET | `/{id}` | Get order by ID |
| GET | `/external/{id}` | Sync status from external API, update DB |
| POST | `/` | Create new order (local + external emission) |
| POST | `/{id}/download` | Return codes as downloadable text file |
| POST | `/external/{id}/process` | Submit order for processing in external system |
| POST | `/external/{id}/download` | Fetch codes from external emission |

---

## KEY BUSINESS LOGIC

### Order Creation Flow
1. Validate product exists
2. Set default factory + marking line if not provided
3. Create `CodeOrder` record in DB (status = 0)
4. Wait 500ms (ensures DB commit before external call)
5. Call external emission API to register codes
6. If success → update status to 1 + store ExternalGuid
7. If fail → update status to -1 + store error in StatusMessage

### Status Sync Flow (`GetExternalOrderByIdAsync`)
1. Fetch current order from DB
2. Call external API for current status
3. Map external status to internal status code
4. Append new `StatusHistory` entry
5. Save to DB

### Code Download Flow
1. Fetch codes from external API
2. Store in `CodeOrderContent` with download metadata
3. Track download count and timestamp
4. Return as `text/plain` file response

### Type Routing (important!)
- `Type == 3` → Container/aggregation endpoints (different external API path)
- `Type != 3` → Standard marking code emission endpoints

---

## CONFIGURATION

### `appsettings.json` (gitignored in Api project — never commit)
```json
{
  "ConnectionStrings": {
    "LocalDb": "Host=localhost;Port=5432;Database=postgres;Username=postgres;Password=postgres",
    "ExternalDb": "Data Source=...;Initial Catalog=externaldb;Integrated Security=True"
  },
  "TJConnection": {
    "BaseURL": "https://pub-api.mark.tj:5230",
    "Token": "<plain text credentials — Base64 encoded before sending>"
  }
}
```

### Logging
- Console output (all levels)
- File: `%ProgramData%\TJConnectorAPI\servicelog.txt` (daily rolling)
- Suppressed: EF Core SQL commands, ASP.NET hosting diagnostics

---

## GITIGNORED FILES (important — never recreate with real values)
- `/TJConnector.Api/appsettings.json` — contains DB credentials + API token
- `/TJConnector.StateSystem/ContainerContentQuery.sql` — sensitive SQL
- `/TJConnector.StateSystem/ContainerInfoQuery.sql` — sensitive SQL

---

## PATTERNS & CONVENTIONS

1. **All I/O is async** — use `async/await` throughout, no `.Result` or `.Wait()`
2. **Service interfaces** — every service has an `IServiceName` interface, registered in DI
3. **Entity configurations** — use `IEntityTypeConfiguration<T>`, never configure in `OnModelCreating` directly
4. **JSONB for arrays** — status history and content arrays are stored as JSONB via Newtonsoft.Json conversion
5. **CustomResult<T>** — standardized response wrapper for API results
6. **Status history append-only** — never remove history entries, only append new ones
7. **MassTransit consumers** — event-driven operations use consumers, not direct service calls
8. **HTTP resilience** — all external API calls go through `CustomHttpClient` (has Polly policies)
9. **Blazor services** — Web project has its own service interfaces (`IBatchServiceWeb` etc.) wrapping HTTP calls to API

---

## HOW TO RUN

### Prerequisites
- .NET 8 SDK
- PostgreSQL 15 running on `localhost:5432`
- SQL Server (for legacy external DB queries, optional)
- Valid `appsettings.json` in `TJConnector.Api/` (see config section)

### Start API
```bash
cd TJConnector.Api
dotnet run
# API at http://localhost:5166
# Swagger at http://localhost:5166/swagger (dev only)
```

### Start Web Frontend
```bash
cd TJConnector.Web
dotnet run
# UI at http://localhost:5xxx → /batches
```

### Database
- Schema is auto-created on first API run (`EnsureCreated()`)
- No manual migration needed for development
- For production: run EF Core migrations from `TJConnector.Api` assembly

---

## KNOWN STATE / THINGS TO WATCH

- `SignalR` hub and `ResponseCompression` are **commented out** in `Program.cs` — infrastructure exists for real-time updates but is inactive
- `ExternalDb` (SQL Server) access is optional — only used for container content/info queries
- The 500ms delay in `CreateOrderAsync` is intentional — don't remove it
- `MigrationsAssembly("TJConnector.Api")` must stay in the Postgres project's EF config
- Blazor Web communicates with API via HTTP — keep API port consistent with Web's `appsettings.json`
