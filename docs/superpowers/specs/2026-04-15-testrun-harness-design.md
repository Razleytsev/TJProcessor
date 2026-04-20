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

### Emission flow — CURRENT reality (2026-04-15, user-verified)

The marking authority has simplified emission statuses down to only two active values:

- **Status `1`** — after `CreateCodeEmission` / `CreateContainerEmission` returns, the document is at status 1 and can be processed immediately. No poll loop needed after creation.
- **Status `6`** — after `ProcessCodeEmission` / `ProcessContainerEmission` returns, the document transitions to status 6 and codes are downloadable.

Flow per emission:
1. `CreateCodeEmission` → receive uuid (document is status 1)
2. `ProcessCodeEmission` with that uuid → external moves it to status 6
3. `GetEmissionInfo` — poll until `status == 6` (usually immediate, but bounded retry for safety)
4. `GetCodesFromEmission` → receive codes

For mastercase, same shape but with `CreateContainerEmission` / `ProcessContainerEmission` / `GetContainerEmissionInfo` / `GetCodesFromContainerEmission` and the simplified body described in the next section.

### Application flow — UNCHANGED (mirrors existing `Transit/5 ApplicationStatusConsumer.cs`)

1. `CreateCodeApplication` → receive uuid (document typically at status 1 = "approved, ready to process")
2. Poll `GetCodeApplicationInfo`:
   - `status == 1` → call `ProcessCodeApplication`, then keep polling
   - `status == 3` → external still processing, keep polling (linear backoff)
   - `status == 5` → ready/done, proceed to next stage
   - `status in {0, 2, 4}` → fail
3. `ProcessCodeApplication` is called exactly once (when we first see status 1)

### Aggregation flow — UNCHANGED (mirrors existing `Transit/8 AggregationStatusConsumer.cs`)

Same shape as application, but with `ContainerOperation`, `ContainerOperationCheck`, `ContainerOperationProcess`.

1. `ContainerOperation` → receive uuid
2. Poll `ContainerOperationCheck`:
   - `status == 1` → call `ContainerOperationProcess`, then keep polling
   - `status == 3` → still processing, keep polling
   - `status == 5` → reported (terminal success)
   - `status in {0, 2, 4}` → fail

### Stage summary

| Stage | What happens | External endpoints hit |
|:---:|:---|:---|
| 1 Pack codes | Create pack emission → process → poll until status 6 → download | `CreateCodeEmission` (type=0, format=0), `ProcessCodeEmission`, `GetEmissionInfo`, `GetCodesFromEmission` |
| 2 Bundle codes | Same, type=1, format=1 | `CreateCodeEmission` (type=1, format=1), `ProcessCodeEmission`, `GetEmissionInfo`, `GetCodesFromEmission` |
| 3 Mastercase SSCC | Create (minimal body), process, poll, download | **NEW** `CreateContainerEmissionMinimal({codesCount:1, type:0})`, `ProcessContainerEmission`, `GetContainerEmissionInfo`, `GetCodesFromContainerEmission` |
| 4 Application | Submit, poll, process-on-status-1, poll until status 5 | `CreateCodeApplication`, `GetCodeApplicationInfo`, `ProcessCodeApplication` |
| 5 Aggregation | Submit, poll, process-on-status-1, poll until status 5 | `ContainerOperation`, `ContainerOperationCheck`, `ContainerOperationProcess` |

Every service method above already exists in `IExternalEmission` and `IExternalContainer` EXCEPT the new minimal container-emission method — see next section.

## User flow

1. User navigates to `/test-run` directly via URL. **The page is NOT added to `NavMenu.razor`** — it's accessible by full URL only so casual users don't stumble into it. Availability is further gated by an `appsettings` flag (see config section).
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

### New DTO: minimal container emission body

User-confirmed: the container emission endpoint `container/emission` accepts the body `{"codesCount": 3, "type": 0}` — nothing else. The existing `CreateContainerEmission(EmissionCreateRequest)` sends extra fields (productUuid, markingLineUuid, factoryUuid, format) which may be tolerated but are not part of the spec. **We leave that existing method untouched** to avoid regressing production code, and add a new minimal DTO + method used only by the test harness:

```csharp
// TJConnector.StateSystem/Model/ExternalRequests/Container/ContainerEmissionCreateRequest.cs
public class ContainerEmissionCreateRequest
{
    public int codesCount { get; set; }
    public sbyte type { get; set; } = 0;
}

// Added to IExternalEmission (container emissions live in the emission service, not the container service)
Task<CustomResult<DocumentCreateResponse>> CreateContainerEmissionMinimal(ContainerEmissionCreateRequest body);
```

Implementation mirrors the existing `CreateContainerEmission` but posts `ContainerEmissionCreateRequest` instead.

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

### External ready/fail status values — USER-CONFIRMED (2026-04-15)

```csharp
public static class TestRunStatusContract
{
    // ─── Emission (packs, bundles, mastercase) ───
    // Only two active statuses: 1 after create, 6 after process.
    public const int EmissionAfterCreate  = 1;  // expected right after CreateCodeEmission returns
    public const int EmissionReady        = 6;  // expected after ProcessCodeEmission completes
    public static readonly int[] EmissionPollAgain = []; // transition is immediate; no intermediate "still working" state
    public static readonly int[] EmissionFail      = []; // any non-{1,6} value at the wrong moment is treated as "unexpected"

    // ─── Application (unchanged, mirrors ApplicationStatusConsumer.cs:42-92) ───
    public const int ApplicationApproved  = 1;  // approved, call Process now
    public const int ApplicationProcessing= 3;  // still working, poll again
    public const int ApplicationReady     = 5;  // ready/done, proceed to aggregation
    public static readonly int[] ApplicationFail = [0, 2, 4]; // saved-error, archived, failed

    // ─── Aggregation (unchanged, mirrors AggregationStatusConsumer.cs:38-88) ───
    public const int AggregationApproved  = 1;  // approved, call Process now
    public const int AggregationProcessing= 3;  // still working, poll again
    public const int AggregationTerminal  = 5;  // reported — final success
    public static readonly int[] AggregationFail = [0, 2, 4];
}
```

The emission flow under the new contract is deterministic — after `ProcessCodeEmission` the status should flip to 6 on the next `GetEmissionInfo` call. The poll loop is kept in case of a network/timing hiccup but with a short cap (5 attempts × 2 s linear backoff, not the 20×exponential used for application/aggregation).

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

## Reprocess semantics — clone forward (with parent history)

"Reprocess from stage N" does **not** mutate the original run. It:

1. Creates a brand-new `TestRun` row with:
   - Same input (PackProductId, BundleProductId, counts, factory, line, location).
   - `ClonedFromTestRunId = original.Id`
   - `ClonedFromStage = N`
   - Prior stages' outputs **copied from the original** (emission guids, codes, SSCC) so stages < N are marked OK without re-minting.
   - `PhaseHistory` **deep-copied from the parent** up through the last OK entry of stage N-1 (per user: "Clone, please, don't start fresh"). A separator entry is appended to the copy: `{ Phase: 0, Name: "— cloned from run #X at stage N —", Outcome: "OK" }`.
   - `Stage` set to N-1 so the first published message advances into stage N.
2. Publishes the stage N message.
3. Returns the new run's `Id`.

Rationale: the external marking authority mints real codes on every emission call. Retrying stage 2 in place would mint a second batch of bundle codes, doubling the consumption. Cloning forward reuses the already-minted artifacts from the failed parent and preserves the full audit trail.

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

## Decisions (all confirmed by user 2026-04-15)

| # | Decision | Source |
|:---:|:---|:---|
| D1 | Separate `TestRun` entity, not a flag on `Package` / `CodeOrder` | Spec — "different page" |
| D2 | MassTransit consumer chain, not hosted service | Matches existing `Transit/*` patterns |
| D3 | 5 consumers, one per stage | Consistent granularity |
| D4 | Reprocess clones forward with **parent phase-history copied** | User: "Clone, please, don't start fresh" |
| D5 | Mastercase uses NEW minimal DTO `ContainerEmissionCreateRequest {codesCount, type}` — no productUuid, no factory, no marking line, no format | User: "complete body is {codesCount: 3, type: 0}" |
| D6 | Pack `format=0`, bundle `format=1` on `EmissionCreateRequest` | Reuses existing `TJConnection:EmissionCodeFormat` config |
| D7 | `applicationDate` and `productionDate` both `DateTimeOffset.UtcNow` (no -4h shift) | User: "Use UtcNow (no shift)" |
| D8 | `type=2`, `result=0` on application body | Known-working values from existing `EmissionServiceConsumer` |
| D9 | Factory / marking line / location selectable on form, auto-select if only one | Matches existing batch modal UX |
| D10 | Confirm switch required before Start | Safety — real marking authority has no rollback |
| D11 | Count inputs accept any value ≥ 1; no multiple-of-1000 constraint | User: "For test we need very small numbers" |
| D12 | **Emission polling:** after `ProcessCodeEmission`, poll `GetEmissionInfo` until `status == 6`, max 5 attempts × 2 s (linear). | User: "We only have two statuses now. Send emission request, check if status 1, process request, check if status 6 then we can download codes." |
| D13 | **Application polling:** unchanged — mirrors `ApplicationStatusConsumer`: status 1 → call Process, status 3 → keep polling, status 5 → ready, {0,2,4} → fail. Linear 6 s × attempt, cap 60 s, max 20 attempts. | User: "Application and aggregation didn't change, failed/keep polling didn't change" |
| D14 | **Aggregation polling:** unchanged — mirrors `AggregationStatusConsumer`: same shape as application. | Same |
| D15 | Page lives at `/test-run`, **NOT added to NavMenu**, gated by `TestRun:Enabled` config flag | User: "don't show it in nav menu, we just need to go there using full url" + "add to appsettings availability of this page" |
| D16 | Auto-refresh interval 2 s, stops when all visible runs are terminal | Matches Batches.razor pattern |
| D17 | Phase history stored as JSONB array on the `TestRun` row | Matches `CodeOrder.StatusHistoryJson` pattern |
| D18 | Reprocess clone copies parent stage outputs byte-for-byte, no re-verification | If parent succeeded through stage N, stage N's output is valid |
| D19 | Consumers use `Task.Delay` for polling inside the consume method | Simpler than MassTransit scheduled redelivery |
| D20 | Cancellation is cooperative — consumer checks `Stage < 0` at each phase boundary | No hard kill mid-phase |
| D21 | No SignalR wiring — UI polls via HTTP | SignalR hub is commented out in API |
| D22 | No retention / cleanup of old `TestRun` rows | User: "I don't care about old testrun rows let them pile up" |
| D23 | No explicit auth beyond the `TestRun:Enabled` appsettings flag + confirm switch | User: "about UX: everything is fine" |

### New config key

```json
"TestRun": {
  "Enabled": true
}
```

Added under the root of `appsettings.json`. If `false`, the API's `POST /api/testrun` and `GET /api/testrun/*` endpoints return 404. Also, the Web page's `@page "/test-run"` checks the config via an injected `IConfiguration` at render time and shows a "Test Run is disabled in this environment" message if off. Default in the committed `appsettings.json` is `true` for dev; production deploys should set it to `false` unless explicitly needed.

## Open risks

1. **Emission polling may still need tuning.** D12 assumes the post-process status flip is near-instant. If it actually takes a second or two in production, the 5-attempt × 2 s = 10 s cap should be enough; if not, we'll tune the constants without touching logic.
2. **Application / aggregation mapping stale.** User confirmed these are unchanged "as far as I understand" and we'll verify later. If they've changed quietly, the first real test run will show the raw response in the phase log — diagnostic gold.
3. **Time budget.** Application + aggregation stages with full linear-backoff polling = up to ~20 minutes per stage in the worst case. Realistic is seconds. If a stage genuinely hangs, the page shows "polling attempt N" until the 20-attempt cap.
4. **Concurrent runs** sharing the same `factoryUuid` — no coordination. The marking authority should handle parallel emission requests fine, but if there's a per-factory serialization requirement, v1 will tangle. Deferred.
5. **EF migration.** `EnsureCreated()` in dev handles the new table automatically. Production deploys need a manual migration; the plan documents this.

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

Implementation plan: `docs/superpowers/plans/2026-04-15-testrun-harness.md`. All decisions are user-confirmed (see Decisions table above); spec and plan are executed autonomously per user's 2026-04-15 instruction to "do everything to the end".
