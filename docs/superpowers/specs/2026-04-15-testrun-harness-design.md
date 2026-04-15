# TestRun Harness — Design Spec

**Date:** 2026-04-15  
**Author:** brainstormed autonomously while user was away; every decision flagged below as either CONFIRMED (from user's prior message) or ASSUMED (default chosen by me, override if wrong).

---

## Goal

A dedicated page that drives the **full production chain** against the real marking authority, end-to-end, in a single guided run. The user supplies two GTINs (pack, bundle) plus two counts (packs-per-bundle, bundles-per-container) and the system exercises every real external API call — emission, application, aggregation — without touching the legacy SQL Server bridge that normal batches go through.

Primary use: **regression testing** against live marking authority changes (such as the 6→4 status renumbering we just hit). Secondary use: onboarding / training / debugging.

## Non-goals

- Not a replacement for normal batch flow — lives in its own page, its own tables, its own URLs. Production flows are untouched.
- Not a load tester — runs one test at a time, single small input.
- Not a sandbox — hits the **real** marking authority with **real** codes. The codes minted during a test run are real and cannot be un-minted. This is intentional (the point is to verify the real contour).
- No legacy SQL Server reads. No `PackageRequest`. No `ContainerContentQuery.sql`.

## Context — what the chain actually does

Confirmed by user's message:

> 1. Receive gtins and counts  
> 2. Request codes from external system for packs, bundles, and 1 sscc.  
> 3. Receive it as usual.  
> 4. Use any order to send application.  
> 5. Send aggregation bundles to sscc.

Expanded into the real API sequence (each external call is a proper `CustomHttpClient` round-trip with Polly resilience):

| Stage | What happens | External endpoints hit |
|:---:|:---|:---|
| 1 Pack codes | Create pack emission → poll until ready → process → download codes | `CreateCodeEmission` (type=0, format=0), `GetEmissionInfo`, `ProcessCodeEmission`, `GetCodesFromEmission` |
| 2 Bundle codes | Same shape, for bundles | `CreateCodeEmission` (type=1, format=1), `GetEmissionInfo`, `ProcessCodeEmission`, `GetCodesFromEmission` |
| 3 Mastercase SSCC | Same shape, for the single SSCC | `CreateContainerEmission` (type=0, productUuid=null), `GetContainerEmissionInfo`, `ProcessContainerEmission`, `GetCodesFromContainerEmission` |
| 4 Application | Assemble pack→bundle mapping, submit, poll, process | `CreateCodeApplication`, `GetCodeApplicationInfo`, `ProcessCodeApplication` |
| 5 Aggregation | Bundles → SSCC, submit, poll, process | `ContainerOperation`, `ContainerOperationCheck`, `ContainerOperationProcess` |

Every service method above already exists in `IExternalEmission` and `IExternalContainer` and works today — no new HTTP plumbing.

## User flow

1. User navigates to `/test-run` (new nav entry added under "Submission Requests" and "Product").
2. Form at the top of the page:
   - **Pack GTIN** — autocomplete dropdown from `Products` table (same control as existing Batches modal).
   - **Bundle GTIN** — autocomplete dropdown, same control.
   - **Packs per bundle** — integer ≥ 1.
   - **Bundles per container** — integer ≥ 1.
   - **Factory** — dropdown from `Factories` table; auto-select if only one.
   - **Marking line** — dropdown from `MarkingLines` table; auto-select if only one.
   - **Location** — dropdown from `Locations` table; auto-select if only one.
   - **Confirm switch:** *"I understand this mints real codes on the marking authority and cannot be reverted."* Start button disabled until this is toggled.
3. User clicks **Start**. Web posts to `POST /api/testrun`. Backend creates a `TestRun` row with Stage 0, publishes the first stage message, and returns the row's `Id`.
4. The page inserts the new run at the top of a **Runs list** below the form and auto-polls `GET /api/testrun/{id}` every 2 seconds while any run on the page is in non-terminal state.
5. Each run in the list is expandable, showing a **5-stage timeline** with per-stage status badges (pending / running / ok / fail) and — inside each stage — a list of **phase log entries** (create / poll attempt 1 / poll attempt 2 / process / download) with raw external request + response JSON blobs inspectable.
6. If any stage fails, a **"Reprocess from stage N"** button appears. Clicking it **clones the run forward** (see reprocess semantics below).

## Data model

One new entity, one value-object array. Added to `ApplicationDbContext` alongside the existing DbSets.

```csharp
// TJConnector.Postgres/Entities/TestRun.cs
public class TestRun {
    public int Id { get; set; }
    public DateTimeOffset RecordDate { get; set; }
    public string? User { get; set; }

    // ─── Input ───
    public int PackProductId { get; set; }
    public int BundleProductId { get; set; }
    public int PacksPerBundle { get; set; }
    public int BundlesPerContainer { get; set; }
    public Guid FactoryUuid { get; set; }
    public Guid MarkingLineUuid { get; set; }
    public Guid LocationUuid { get; set; }

    // ─── Stage / status ───
    // 0 = created; 1..5 = running stage N; 100 = done; -1..-5 = failed at stage N
    public int Stage { get; set; }
    public string? StatusMessage { get; set; }

    // ─── Phase log (append-only, JSONB) ───
    public TestRunPhaseLog[] PhaseHistory { get; set; } = Array.Empty<TestRunPhaseLog>();

    // ─── Stage outputs (filled as we go) ───
    public Guid? PackEmissionGuid { get; set; }
    public Guid? BundleEmissionGuid { get; set; }
    public Guid? MastercaseEmissionGuid { get; set; }
    public string[]? PackCodes { get; set; }          // JSONB — length = PacksPerBundle * BundlesPerContainer
    public string[]? BundleCodes { get; set; }        // JSONB — length = BundlesPerContainer
    public string? MastercaseSscc { get; set; }
    public Guid? ApplicationGuid { get; set; }
    public Guid? AggregationGuid { get; set; }

    // ─── Reprocess lineage ───
    public int? ClonedFromTestRunId { get; set; }
    public int? ClonedFromStage { get; set; }

    public void AppendPhaseLog(TestRunPhaseLog entry)
        => PhaseHistory = [..PhaseHistory, entry];
}

// Value object stored inside PhaseHistory JSONB
public class TestRunPhaseLog {
    public int Stage { get; set; }                    // 1..5
    public string PhaseName { get; set; } = "";       // e.g. "Create pack emission"
    public DateTimeOffset StartedAt { get; set; }
    public DateTimeOffset? FinishedAt { get; set; }
    public string Outcome { get; set; } = "IN_PROGRESS";  // IN_PROGRESS, OK, FAIL
    public string? ExternalRequestJson { get; set; }
    public string? ExternalResponseJson { get; set; }
    public string? Notes { get; set; }
}
```

EF config mirrors the existing `CodeOrder` / `Package` pattern: JSONB columns via Newtonsoft, `RecordDate` default `NOW()`. Config file: `TJConnector.Postgres/Configurations/TestRunConfiguration.cs`.

`EnsureCreated()` in `Program.cs` will pick up the new table automatically in dev. No migration file unless user wants explicit migration for prod.

## Architecture — MassTransit consumer chain

Mirrors the existing `Transit/*` pattern exactly. Five consumers, one per stage. Each consumer handles the full lifecycle of its stage (create → poll → process → download) internally, logs each phase into `TestRun.PhaseHistory`, then publishes the next stage's message on success or marks the run as failed on error.

Message types (all take a single `TestRunId`; consumers load the row fresh each time):

```csharp
public record TestRunStart               { public int TestRunId { get; init; } }
public record TestRunStage2Bundles       { public int TestRunId { get; init; } }
public record TestRunStage3Mastercase    { public int TestRunId { get; init; } }
public record TestRunStage4Application   { public int TestRunId { get; init; } }
public record TestRunStage5Aggregation   { public int TestRunId { get; init; } }
```

No explicit "done" message — the aggregation consumer sets `Stage = 100` on its own successful exit. The Web page infers done-state from `Stage == 100`.

Consumers (all in `TJConnector.Api/TestRun/`):

1. `EmitPacksConsumer : IConsumer<TestRunStart>`
2. `EmitBundlesConsumer : IConsumer<TestRunStage2Bundles>`
3. `EmitMastercaseConsumer : IConsumer<TestRunStage3Mastercase>`
4. `SubmitApplicationConsumer : IConsumer<TestRunStage4Application>`
5. `SubmitAggregationConsumer : IConsumer<TestRunStage5Aggregation>`

Each consumer internally:

```csharp
// Pseudo-code for EmitPacksConsumer.Consume
var run = await _context.TestRuns.FindAsync(msg.TestRunId);
if (run is null || run.Stage is < 0 or >= 100) return;   // cancelled / done

run.Stage = 1;
await LogPhaseStart(run, stage: 1, phase: "Create pack emission");

var createResp = await _emission.CreateCodeEmission(new EmissionCreateRequest {
    codesCount = run.PacksPerBundle * run.BundlesPerContainer,
    productUuid = await GetProductExternalUid(run.PackProductId),
    factoryUuid = run.FactoryUuid,
    markingLineUuid = run.MarkingLineUuid,
    Type = 0,
    format = 0
});
await LogPhaseEnd(run, outcome: createResp.Success ? "OK" : "FAIL",
                  reqJson: SerializeRequest(...), respJson: SerializeResponse(createResp));
if (!createResp.Success) { run.Stage = -1; /* save + return */; }
run.PackEmissionGuid = createResp.Content.uuid;

// Poll loop — exponential backoff like ApplicationStatusConsumer
for (int attempt = 1; attempt <= MAX_POLL_ATTEMPTS; attempt++) {
    await LogPhaseStart(run, stage: 1, phase: $"Poll emission (attempt {attempt})");
    var info = await _emission.GetEmissionInfo(run.PackEmissionGuid.Value);
    await LogPhaseEnd(run, ...);
    if (info.Content?.status == EXT_READY_STATUS) break;
    if (info.Content?.status == EXT_FAIL_STATUS) { run.Stage = -1; return; }
    await Task.Delay(Math.Min(attempt * 6000, 60000));
}

// Process
await LogPhaseStart(run, stage: 1, phase: "Process emission");
var processResp = await _emission.ProcessCodeEmission(new ProcessDocument { uuids = [run.PackEmissionGuid.Value] });
await LogPhaseEnd(run, ...);
if (!processResp.Success) { run.Stage = -1; return; }

// Download
await LogPhaseStart(run, stage: 1, phase: "Download codes");
var codesResp = await _emission.GetCodesFromEmission(new DownloadCodesRequest {
    type = 0, uuid = run.PackEmissionGuid.Value
});
await LogPhaseEnd(run, ...);
if (!codesResp.Success || codesResp.Content?.codes is null) { run.Stage = -1; return; }
run.PackCodes = codesResp.Content.codes;

await _context.SaveChangesAsync();
await _publish.Publish(new TestRunStage2Bundles { TestRunId = run.Id });
```

Every other consumer follows the same shape — only the external calls and the body assembly differ.

### Application body assembly (Stage 4)

**Positional pairing note:** in the real chain, a bundle's pack list comes from the legacy SQL Server and reflects physical manufacturing reality. In a synthetic test run there is no physical pairing — we just minted 6 pack codes and 2 bundle codes, and we pair them by index. The marking authority has no way to verify the pairing corresponds to anything physical; it only checks that the codes are well-formed, belong to the factory, and haven't been used before. So positional assignment is arbitrary but correct for a test.

```csharp
// Pack codes come back length = PacksPerBundle * BundlesPerContainer, already in order.
// Bundle codes come back length = BundlesPerContainer, already in order.
// Pair them positionally: first PacksPerBundle packs → first bundle, and so on.
var groupCodes = new List<GroupCode>(run.BundlesPerContainer);
for (int i = 0; i < run.BundlesPerContainer; i++) {
    var bundleWithGs = GS1CodeHelper.TryInsertGroupSeparator(run.BundleCodes[i], out var gs) ? gs : run.BundleCodes[i];
    groupCodes.Add(new GroupCode {
        groupCode = bundleWithGs,
        codes = run.PackCodes.Skip(i * run.PacksPerBundle).Take(run.PacksPerBundle).ToArray()
    });
}

var applicationBody = new ApplicationCreateRequest {
    applicationDate = DateTimeOffset.UtcNow,
    productionDate = DateTimeOffset.UtcNow,
    factoryUuid = run.FactoryUuid,
    markingLineUuid = run.MarkingLineUuid,
    locationUuid = run.LocationUuid,
    groupCodes = groupCodes,
    result = 0,
    type = 2
};
```

### Aggregation body (Stage 5)

```csharp
// Bundles (with GS already inserted) aggregated into the minted SSCC.
var aggregationBody = new ContainerOperationCreateRequest {
    codes = run.BundleCodes.Select(b => GS1CodeHelper.TryInsertGroupSeparator(b, out var gs) ? gs : b).ToArray(),
    containerCode = run.MastercaseSscc,
    locationUuid = run.LocationUuid,
    transferCodes = [],
    type = 0
};
```

### Polling constants

```csharp
const int MAX_POLL_ATTEMPTS = 20;
const int POLL_BACKOFF_SECONDS = 6;  // first delay, grows linearly up to 60s
```

Matches the existing `ApplicationStatusConsumer` / `AggregationStatusConsumer` rhythm.

### External ready/fail status values

**CRITICAL GAP** — these are the constants the poll loops compare against. They depend on the **new** external marking authority status scheme that triggered the session's earlier regression. My defaults:

```csharp
// Emission flow (CreateCodeEmission, CreateContainerEmission)
const int EXT_EMISSION_READY = 4;          // per user's prior message: "Success was 6 before, is 4 now"
static int[] EXT_EMISSION_POLL_AGAIN = [1, 3];  // approved, processing — keep polling
static int[] EXT_EMISSION_FAIL       = [0, 2];  // not approved, archived — stop

// Application flow (CreateCodeApplication + GetCodeApplicationInfo)
// Mirrors the existing ApplicationStatusConsumer's interpretation until user confirms otherwise.
const int EXT_APPLICATION_READY      = 5;   // "ready, next step" in ApplicationStatusConsumer:85
static int[] EXT_APPLICATION_POLL_AGAIN = [1, 3];
static int[] EXT_APPLICATION_FAIL       = [0, 2, 4];

// Aggregation flow (ContainerOperation + ContainerOperationCheck)
// Mirrors AggregationStatusConsumer.
const int EXT_AGGREGATION_READY      = 5;
static int[] EXT_AGGREGATION_POLL_AGAIN = [1, 3];
static int[] EXT_AGGREGATION_FAIL       = [0, 2, 4];
```

All these are centralised in a single `TestRunStatusContract` static class so the user can correct them in one place once the external team's status table is known. See questions Q1–Q4 in **Inputs required from you** at the bottom of this doc.

## API endpoints

New controller `TJConnector.Api/Controllers/TestRunController.cs`.

| Method | Route | Body | Returns |
|:---:|:---|:---|:---|
| `POST` | `/api/testrun` | `TestRunCreateForm` (input fields) | `TestRunDto` with `Id` and `Stage=0` |
| `GET` | `/api/testrun` | — | `TestRunDto[]` (paginated, recent first) |
| `GET` | `/api/testrun/{id}` | — | `TestRunDto` with full phase history |
| `POST` | `/api/testrun/{id}/reprocess/{fromStage}` | — | new `TestRunDto` cloned forward |
| `POST` | `/api/testrun/{id}/cancel` | — | updated `TestRunDto` |

`TestRunCreateForm` (goes in `SharedLibrary/DTOs/Forms`):

```csharp
public class TestRunCreateForm {
    public int PackProductId { get; set; }
    public int BundleProductId { get; set; }
    public int PacksPerBundle { get; set; }
    public int BundlesPerContainer { get; set; }
    public Guid FactoryUuid { get; set; }
    public Guid MarkingLineUuid { get; set; }
    public Guid LocationUuid { get; set; }
    public string User { get; set; } = "";
}
```

`TestRunDto` (goes in `SharedLibrary/Models`) — projection of `TestRun` with product GTINs resolved into strings for easy display.

## Reprocess semantics — clone forward

"Reprocess from stage N" does **not** mutate the original run. It:

1. Creates a brand-new `TestRun` row with:
   - Same input (PackProductId, BundleProductId, counts, factory, line, location).
   - `ClonedFromTestRunId = original.Id`
   - `ClonedFromStage = N`
   - Prior stages' outputs **copied from the original** (emission guids, codes, SSCC) so stages < N are marked OK without re-minting.
   - Stage set to N-1 so the first published message will advance into stage N.
2. Publishes the stage N message.
3. Returns the new run's `Id`.

Rationale: the external marking authority mints real codes on every emission call. Retrying stage 2 in place would mint a second batch of bundle codes, doubling the consumption. Cloning forward reuses the already-minted artifacts from the failed parent.

UI makes this visible: cloned runs show a "Cloned from #123 at stage 3" badge and link back to the parent.

## Page layout

```
┌─────────────────────────────────────────────────────────────┐
│ Test Run ↻ 2 s                              [ + Start Run ] │
├─────────────────────────────────────────────────────────────┤
│ [form: Pack GTIN, Bundle GTIN, counts, Factory, Line,       │
│        Location, confirm switch]                            │
├─────────────────────────────────────────────────────────────┤
│ Recent runs                                                 │
│ ┌─────┬──────────────┬──────────────┬──────┬─────────────┐  │
│ │ ID  │ Pack GTIN    │ Bundle GTIN  │ Size │ State       │  │
│ ├─────┼──────────────┼──────────────┼──────┼─────────────┤  │
│ │ 3   │ 0000...0014  │ 0000...0021  │ 6/2  │ ● Stage 4   │  │
│ │ 2   │ 0000...0014  │ 0000...0021  │ 6/2  │ ✓ Done      │  │
│ │ 1   │ 0000...0014  │ 0000...0021  │ 2/1  │ ✗ Failed S2 │  │
│ └─────┴──────────────┴──────────────┴──────┴─────────────┘  │
│ (expanded row shows 5-stage timeline + phase log)           │
└─────────────────────────────────────────────────────────────┘
```

Expanded timeline:

```
Stage 1 — Pack emission     ✓ OK     1.2s
  ├─ Create               OK 120ms   [req] [resp]
  ├─ Poll (1)             OK 210ms   [req] [resp]
  ├─ Process              OK 180ms   [req] [resp]
  └─ Download codes       OK 340ms   [req] [resp]

Stage 2 — Bundle emission   ✓ OK     0.9s
  └─ ...

Stage 3 — Mastercase SSCC   ✓ OK     0.7s
Stage 4 — Application       ● Running (polling attempt 3)
Stage 5 — Aggregation       ○ Pending

[Reprocess from stage 4]
```

`[req]` and `[resp]` are click-to-expand JSON blocks.

## Error handling

Same disciplined pattern the existing consumers use:

- Any external call that returns `Success=false` → set `run.Stage = -currentStage`, write the response into `StatusMessage`, append a FAIL phase log, SaveChanges, **do not publish** the next stage message. UI's auto-refresh picks up the failure within 2 s.
- Unexpected exceptions inside a consumer → caught at the top level, logged via `ILogger`, marked as FAIL with exception message. MassTransit in-memory transport doesn't retry on throw; this is a deliberate choice (retrying blind against the real marking authority would double-mint codes).
- Cancel button posts `POST /api/testrun/{id}/cancel` which sets `Stage = -99` (user-cancelled) and appends a log entry. Consumers check `Stage < 0` on entry and bail out quietly.
- Hard timeout: max poll attempts (20) with backoff capped at 60 s = worst case ~10 minutes per stage. On exhaustion, marks FAIL with "Max poll attempts exceeded — external system may be stuck."

## Testing

- **Unit:** `EmitPacksConsumer` and friends are awkward to unit-test because they couple external HTTP, EF, and MassTransit publishing. Instead, **extract pure helpers** into testable classes:
  - `TestRunBodyBuilder` — static methods to build `EmissionCreateRequest`, `ApplicationCreateRequest`, `ContainerOperationCreateRequest` from a `TestRun`. Tests cover pack→bundle mapping, GS insertion, type flags.
  - `TestRunPhaseLogger` — append-only log helper. Tests cover phase-transition ordering and JSON serialization.
- **Integration (manual):** run against the real marking authority with a small test (packsPerBundle=2, bundlesPerContainer=1). User-driven; I can't run this safely.
- **No mocking of the external API in tests.** The whole point of this feature is to verify real behavior.

## Out of scope (explicitly)

- Concurrent runs — one user, one run at a time. Running multiple doesn't break anything but the UX isn't optimised for it.
- Parallelism within a run — stages 1, 2, 3 (all three emissions) are logically parallel and could run concurrently, but v1 runs them sequentially for simpler state tracking.
- Export/download of the generated codes as a file — the raw arrays are visible in the JSON response blocks; if you want a download button that's a 15-minute follow-up.
- Retention / cleanup — old runs accumulate. If you want auto-deletion of runs > 30 days, that's a follow-up.

## Assumptions made (override any of these)

| # | Assumption | Why I chose it |
|:---:|:---|:---|
| A1 | Separate `TestRun` entity, not a flag on `Package` / `CodeOrder` | User said "different page" — dedicated entity keeps test traffic out of production reports and allows cleaner reprocess-clone semantics |
| A2 | MassTransit consumer chain, not a hosted service or long-running endpoint | Matches every existing batch/package flow in the codebase |
| A3 | 5 consumers, one per stage, each driving create→poll→process→download internally | Compromise between granularity and consumer-file sprawl; each consumer is ~150 lines |
| A4 | Reprocess clones forward, not in-place retry | In-place retry would double-mint codes on the real marking authority |
| A5 | Mastercase emission uses `CreateContainerEmission` with `productUuid=null` | User said "mastercase doesn't need gtin"; `CreateContainerEmission` takes a nullable productUuid |
| A6 | Pack `format=0`, bundle `format=1` | Matches the config we already added in `TJConnection:EmissionCodeFormat` |
| A7 | `applicationDate` and `productionDate` both set to `DateTimeOffset.UtcNow` (no -4h shift) | Test runs are controlled; consistent UTC is the cleanest default. If the real chain needs the -4h shift, we can copy that behavior — see open question |
| A8 | `type=2` on application body, `result=0`, matches existing `EmissionServiceConsumer` | Known-working values from the production flow |
| A9 | Factory / marking line / location selectable on the form, auto-select if only one | Matches existing batch modal UX; lets test runs target different env config |
| A10 | Confirm switch required before Start (explicit "I know this mints real codes") | Safety — the real marking authority has no rollback |
| A11 | No "multiple of 1000" constraint on the counts | Test runs are typically small (e.g. 4 packs × 2 bundles = 8 codes). If the marking authority rejects small counts, we'll learn that on the first test and can adjust. **Open for confirmation** |
| A12 | Polling: linear backoff 6s × attempt, cap 60s, max 20 attempts | Mirrors `ApplicationStatusConsumer` |
| A13 | `EXT_READY_STATUS = 4` for emission poll | User said so for the emission creation flow. **Application and aggregation ready statuses are unknown** — see open question |
| A14 | Page lives at `/test-run`, added to `NavMenu.razor` below "Product" | Consistent with existing nav style |
| A15 | Auto-refresh interval 2 s, stops when all visible runs are terminal | Matches Batches.razor pattern |
| A16 | Phase history stored as JSONB array on the `TestRun` row (not a separate table) | Matches `CodeOrder.StatusHistoryJson` pattern; append-only works fine in JSONB |
| A17 | Reprocess clone copies prior stage outputs byte-for-byte, no re-verification | If the parent succeeded through stage N, stage N's output is valid; re-verifying would waste external API quota |
| A18 | Consumers use `Task.Delay` for polling inside the consume method, not MassTransit's scheduled redelivery | Simpler; total consume time is bounded by MAX_POLL_ATTEMPTS × 60s ≈ 20 min per stage, well under any reasonable consumer deadline |
| A19 | Cancellation is cooperative — consumer checks `Stage < 0` at each phase boundary | No hard kill mid-phase because we don't want to abandon a half-committed external call |
| A20 | No SignalR wiring for live updates — UI polls via HTTP | SignalR hub is commented out in the API per CLAUDE.md; HTTP polling at 2s feels instant enough for a test page |

## Inputs required from you

These are the things I cannot answer from the codebase alone. Everything in the spec builds on a default for each, but I'd rather have your answer before the code is written than discover I was wrong on the real system.

| # | Question | My default (if you don't answer) |
|:---:|:---|:---|
| Q1 | **What external status value means "ready to process / download" for EMISSION now?** I guessed `4` from our prior conversation. Confirm or correct. | `EXT_READY_STATUS = 4` |
| Q2 | **What external status value means "ready to process" for APPLICATION now?** (`CreateCodeApplication` returns a doc uuid, then we poll `GetCodeApplicationInfo` until a certain status, then call `ProcessCodeApplication`.) Right now `ApplicationStatusConsumer` treats external 5 as success ("ready, trigger aggregation"). Still true? | Assume same as emission: `4` |
| Q3 | **What external status value means "done" for AGGREGATION now?** (`ContainerOperation` → poll `ContainerOperationCheck` → `ContainerOperationProcess` → poll again.) `AggregationStatusConsumer` treats external 5 as terminal success. Still true? | Assume `5` |
| Q4 | **What external status value means "failed" across all three?** There's no single FAIL status in the current switches — each consumer treats specific negative outcomes (0, 2, 4 in the application switch) as failures. For the test harness I need to distinguish "poll again" from "give up." | Treat any value in `{0, 2}` as FAIL; keep polling on `{1, 3}`; stop on `{4, 5}` |
| Q5 | **Does the marking authority accept `codesCount < 1000`?** The existing batch modal enforces a multiple-of-1000 constraint. Is that a business rule or an API constraint? Test runs need to be small (e.g. 6 codes) to be practical. | Allow any count ≥ 1 |
| Q6 | **`applicationDate` timezone:** `EmissionServiceConsumer.cs:85` uses `UtcNow.AddHours(-4)`. The test harness should either mirror that exact behavior or use plain `UtcNow`. Which? | Use `UtcNow` (no shift). |
| Q7 | **Does `CreateContainerEmission` require `productUuid=null` or is it acceptable to omit the field entirely?** You confirmed mastercase doesn't need a GTIN — I'll send `productUuid=null` and rely on `[JsonIgnore(WhenWritingNull)]` (already in place for `format`). Confirm that the serializer emits/omits null the way the external API expects. | Send `null` with `[JsonIgnore(WhenWritingNull)]` |
| Q8 | **Does the test page require authentication?** The existing pages don't — there's no auth middleware in `TJConnector.Api/Program.cs` or `TJConnector.Web/Program.cs`. If you want this gated because it mints real codes, say so. | No auth beyond the confirm-switch |
| Q9 | **`TestRun.User` field — should it default to a fixed value, pull from Windows identity, or require manual input?** | Free-text input field in the form, required, min 2 chars |
| Q10 | **Three emission types — can I assume the same `factoryUuid` / `markingLineUuid` work for pack, bundle, AND mastercase emissions?** Or does mastercase need a different marking line? | Same for all three |
| Q11 | **Count semantics — "packs per bundle × bundles per container" is the pack emission count. But do you want a separate "bundle count" field too, in case you want more bundles than containers?** I'm interpreting your spec as exactly one container, exactly `bundlesPerContainer` bundles, exactly `packsPerBundle × bundlesPerContainer` packs. | Exactly one container per run |
| Q12 | **Should reprocess-forward copy the StatusHistory from the parent or start fresh?** I'm going with fresh — the new run's history shows only what the clone did, and the "Cloned from #X at stage N" badge links to the parent for the earlier history. | Start fresh |
| Q13 | **Retention:** any concerns about TestRun rows piling up, or is housekeeping not a priority right now? | Ignore, no cleanup in v1 |
| Q14 | **Nav label:** the new page should show up as "Test Run" in NavMenu? "Contour Test"? Other? | "Test Run" |

## Open risks

1. **External status unknowns.** Q1–Q4 above. If my defaults are wrong in any of the three flows, the first test run will fail with a FAIL phase log showing the raw response — that's actually a useful diagnostic, but it means the first run will fail by design until the constants are corrected.
2. **Time budget.** Five stages × up to 20 minutes max per stage = theoretical 100 minutes per run. Realistic is 1–5 minutes. If any stage genuinely hangs at the external system, the test page will show "polling attempt N" indefinitely until the 20-attempt cap hits. Acceptable for a test tool.
3. **Concurrent runs** sharing the same `factoryUuid` — no coordination. If two runs start simultaneously the external system receives two parallel emission requests. The marking authority should handle that fine, but if there's a per-factory serialization requirement I don't know about, v1 will tangle. Mitigation: button is disabled while a run with terminal-not-reached state exists for the same user, but I'm leaving that out of v1 unless you ask.
4. **EF migration.** I'm assuming `EnsureCreated()` in dev is fine. For prod, a manual migration is needed. I'll call this out in the plan.

## Rough effort estimate

| Part | Hours |
|:---|:---:|
| `TestRun` entity + EF config + JSONB converter | 1 |
| DTOs, service interface, controller | 1 |
| Body builder + phase logger helpers (pure, testable) | 1 |
| Five consumers | 3 |
| MassTransit registration in `Program.cs` | 0.2 |
| Razor page (form + runs list + timeline + polling) | 2 |
| Unit tests for body builder + logger | 0.5 |
| Wiring & first successful local run | 0.5 |
| **Total** | **~9** |

Slightly higher than my earlier 5–7h estimate because I've added the reprocess-clone semantics and the full phase log plumbing.

## Post-approval next step

Invoke the **superpowers:writing-plans** skill to break this design into a tasked TDD implementation plan. The plan will go to `docs/superpowers/plans/2026-04-15-testrun-harness.md`. No code is written until you approve both this spec AND the plan.
