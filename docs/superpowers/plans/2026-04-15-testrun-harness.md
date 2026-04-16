# TestRun Harness — Implementation Plan

**Based on:** `docs/superpowers/specs/2026-04-15-testrun-harness-design.md`  
**Executed autonomously** on 2026-04-15 per user instruction "do everything to the end".

---

## Task list (executed in order)

### Task 1 — New DTO: `ContainerEmissionCreateRequest`
- File: `TJConnector.StateSystem/Model/ExternalRequests/Container/ContainerEmissionCreateRequest.cs`
- Body: `{ int codesCount; sbyte type = 0; }`

### Task 2 — Extend `IExternalEmission` + `ExternalEmissionService`
- Add method `Task<CustomResult<DocumentCreateResponse>> CreateContainerEmissionMinimal(ContainerEmissionCreateRequest body)` to the interface
- Implement: posts body to `container/emission`, returns `DocumentCreateResponse`, same error pattern as existing `CreateContainerEmission`
- **Leave existing `CreateContainerEmission` untouched** so production code (`OrderService.CreateOrderAsync`) is unaffected

### Task 3 — `TestRun` entity + `TestRunPhaseLog` value object
- File: `TJConnector.Postgres/Entities/TestRun.cs`
- Fields per spec data-model section
- `AppendPhaseLog` helper

### Task 4 — EF config for `TestRun`
- File: `TJConnector.Postgres/Configurations/TestRunConfiguration.cs`
- JSONB conversion for `PhaseHistory`, `PackCodes`, `BundleCodes` via Newtonsoft
- `RecordDate` default `NOW()`
- `HasMaxLength` where sensible

### Task 5 — Register DbSet
- File: `TJConnector.Postgres/ApplicationDbContext.cs`
- Add `public DbSet<TestRun> TestRuns { get; set; }`

### Task 6 — `TestRunStatusContract` static class
- File: `TJConnector.Api/TestRun/TestRunStatusContract.cs`
- Constants per spec section "External ready/fail status values"

### Task 7 — `TestRunBodyBuilder` pure helpers
- File: `TJConnector.Api/TestRun/TestRunBodyBuilder.cs`
- Static methods:
  - `BuildPackEmissionRequest(TestRun, Product pack, int packFormat)` → `EmissionCreateRequest`
  - `BuildBundleEmissionRequest(TestRun, Product bundle, int bundleFormat)` → `EmissionCreateRequest`
  - `BuildMastercaseEmissionRequest(TestRun)` → `ContainerEmissionCreateRequest`
  - `BuildApplicationRequest(TestRun)` → `ApplicationCreateRequest` (positional pack→bundle pairing with GS insertion)
  - `BuildAggregationRequest(TestRun)` → `ContainerOperationCreateRequest` (GS insertion on bundle codes)

### Task 8 — Unit tests for `TestRunBodyBuilder`
- File: `TJConnector.StateSystem.Tests/TestRunBodyBuilderTests.cs`
- Cover: pack→bundle positional pairing, GS insertion on bundles, type flags, format flags, empty/null guards
- Run: `dotnet test`

### Task 9 — `TestRunCreateForm` DTO
- File: `SharedLibrary/DTOs/Forms/TestRunCreateForm.cs`
- Fields per spec API section

### Task 10 — `TestRunDto` shared model
- File: `SharedLibrary/Models/TestRunDto.cs`
- Projection with resolved pack/bundle GTINs

### Task 11 — MassTransit message records
- File: `TJConnector.Api/TestRun/Messages.cs`
- Records: `TestRunStart`, `TestRunStage2Bundles`, `TestRunStage3Mastercase`, `TestRunStage4Application`, `TestRunStage5Aggregation`

### Task 12 — `EmitPacksConsumer`
- File: `TJConnector.Api/TestRun/EmitPacksConsumer.cs`
- Drives: create → process → poll GetEmissionInfo until status 6 (max 5 × 2 s) → download → save codes → publish Stage2

### Task 13 — `EmitBundlesConsumer`
- File: `TJConnector.Api/TestRun/EmitBundlesConsumer.cs`
- Same shape, type=1, format=1

### Task 14 — `EmitMastercaseConsumer`
- File: `TJConnector.Api/TestRun/EmitMastercaseConsumer.cs`
- Uses `CreateContainerEmissionMinimal`, `ProcessContainerEmission`, `GetContainerEmissionInfo`, `GetCodesFromContainerEmission`
- Sets `MastercaseSscc` from the single returned code

### Task 15 — `SubmitApplicationConsumer`
- File: `TJConnector.Api/TestRun/SubmitApplicationConsumer.cs`
- Uses `TestRunBodyBuilder.BuildApplicationRequest`
- Create → loop: poll GetCodeApplicationInfo → on status 1, call ProcessCodeApplication → on status 5, publish Stage5 → fail on {0,2,4}

### Task 16 — `SubmitAggregationConsumer`
- File: `TJConnector.Api/TestRun/SubmitAggregationConsumer.cs`
- Uses `TestRunBodyBuilder.BuildAggregationRequest`
- Create → loop: poll ContainerOperationCheck → on status 1, call ContainerOperationProcess → on status 5, set `Stage = 100` (Done) → fail on {0,2,4}

### Task 17 — `ITestRunService` + `TestRunService`
- File: `TJConnector.Api/Services/TestRunService.cs`
- Methods:
  - `Task<TestRun?> CreateAsync(TestRunCreateForm form)` — validates product IDs exist, creates row, publishes `TestRunStart`
  - `Task<TestRun?> GetByIdAsync(int id)` — with resolved product refs
  - `Task<List<TestRun>> ListAsync(int take = 50)` — recent first
  - `Task<TestRun?> ReprocessAsync(int parentId, int fromStage)` — clone-forward logic
  - `Task<TestRun?> CancelAsync(int id)` — sets Stage = -99

### Task 18 — `TestRunController`
- File: `TJConnector.Api/Controllers/TestRunController.cs`
- Endpoints per spec API section
- Gates all endpoints with `if (!configuration.GetValue<bool>("TestRun:Enabled")) return NotFound();`

### Task 19 — Register service + consumers in `Program.cs`
- File: `TJConnector.Api/Program.cs`
- `builder.Services.AddScoped<ITestRunService, TestRunService>();`
- Add consumers to MassTransit bus registration

### Task 20 — Add `TestRun:Enabled` to appsettings
- File: `TJConnector.Api/appsettings.json` (gitignored — edit only, do not commit)
- Default `true` for dev

### Task 21 — Web service `ITestRunServiceWeb` + `TestRunServiceWeb`
- File: `TJConnector.Web/Services/Contracts/ITestRunServiceWeb.cs`
- File: `TJConnector.Web/Services/Implementation/TestRunServiceWeb.cs`
- HTTP wrapper matching controller

### Task 22 — `TestRun.razor` page
- File: `TJConnector.Web/Pages/TestRun.razor`
- `@page "/test-run"`
- Form: two GTIN dropdowns (Products filtered by Type), packsPerBundle, bundlesPerContainer, factory, marking line, location, user, confirm switch, Start button
- Below: recent runs list with expandable 5-stage timeline showing per-phase log with collapsible JSON blocks
- Auto-refresh every 2 s while any visible run is non-terminal
- `IsEnabled` config check at top — show "disabled" banner if off

### Task 23 — Register Web service
- File: `TJConnector.Web/Program.cs`
- `builder.Services.AddScoped<ITestRunServiceWeb, TestRunServiceWeb>();`

### Task 24 — Build + test
- `dotnet build TJConnector.sln`
- `dotnet test TJConnector.StateSystem.Tests`
- Fix any errors discovered
- No warnings introduced beyond existing baseline

### Task 25 — Incremental commits
- Commit chain matching the logical groups:
  1. Entity + EF + DbSet (Tasks 3,4,5)
  2. Container emission DTO + service method (Tasks 1,2)
  3. TestRun helpers + tests (Tasks 6,7,8)
  4. Shared DTOs (Tasks 9,10)
  5. Consumers + messages (Tasks 11-16)
  6. Service + controller + Program.cs + config (Tasks 17,18,19,20)
  7. Web service + Razor page (Tasks 21,22,23)

Each commit builds cleanly and tests pass. No force pushes. No destructive ops.

---

## Execution note

Executing this plan autonomously. Commits happen as tasks complete. Final message summarises what shipped, what tests pass, and what the user must do to smoke-test against the real marking authority.
