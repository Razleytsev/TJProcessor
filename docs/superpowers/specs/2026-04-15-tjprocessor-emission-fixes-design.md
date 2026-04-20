# TJProcessor — Emission / Application Fixes

**Date:** 2026-04-15
**Scope:** `TJProcessor` repo only (not `TJ8` or other siblings).
**Summary:** Five related fixes against the `pub-api.mark.tj` integration after changes in the external marking system: add `format` to emission requests, strip `\u001d` (GS) from downloaded bundle files, inject GS into pack codes sent to `apply`/aggregate, populate `productionDate` + `locationUuid` on application requests, and fix the emission status mapping so auto-download actually fires.

---

## 1. Background

The external marking API (`pub-api.mark.tj:5230`) changed its code hierarchy and request/response contracts:

- **Hierarchy:** `pack` → `bundle` → `container`. Packs are the smallest marking unit; bundles are GS1 pack codes (contain `\u001d` / GS separator between AI-21 and AI-93); containers are SSCC aggregation units.
- **Emission endpoint `markingCode/emission`** accepts a `type` field in the body distinguishing packs (`type: 0`) from bundles (`type: 1`), and now accepts a new `format` field that selects the returned code format. **Packs use `format: 0`, bundles use `format: 1`.** Containers use a separate endpoint (`container/emission`) with `type: 0` and **no** `format`.
- **Download response shape:** packs come back as clean strings; bundles come back with an embedded `\u001d` between the pack serial and the `93...` block (e.g. `010460020000000021+UyLGXyZ\u001d93444b155b`). See sample files at repo root: `markingCodeemissioncodes packs.txt` and `markingCodeemissioncodes bundles.txt`.
- **Emission status codes:** the "success / ready for download" status used to be `6`; it is now `4`. Consequently the old mapping `6 => 5` in `OrderService.GetExternalOrderByIdAsync` left orders stuck in terminal status `5` without ever triggering auto-download.
- **Application request body** now requires both `productionDate` and `locationUuid`, neither of which the current code sets.
- **Legacy SQL Server `ContentTable`** (queried via `ContainerContentQuery.sql`) stores pack codes **without** the GS. Those same codes are sent to `markingCode/report/apply` and `container/operation/create` and must contain GS there, because the application/aggregation endpoints validate GS1 format.

The users of this service have been unable to automatically download codes, and upload-bundle flows are broken against the new external API.

---

## 2. Non-goals

- Rewriting the status-code enum system across emission / application / aggregation.
- Adding a Location selector to the UI.
- Fixing the unused `case 2` gap in `GetExternalOrderByIdAsync` (external `2 => _ => -4`).
- Migrating off `ContainerContentQuery.sql` or touching the legacy SQL Server schema.
- Any changes to sibling projects (`TJ`, `TJ.Process`, `TJ8`, `StateSystem`).
- Removing or renaming the internal `CodeOrder.Type` enumeration.

---

## 3. Approach

Surgical edits to existing touch-points, plus **one** new helper (`GS1CodeHelper`) that owns all GS insertion/stripping logic so the rule lives in exactly one place. No new DI abstractions, no feature flags, no backwards-compatibility shims. Configuration is added under the existing `TJConnection` section in `appsettings.json`.

A small test project (`TJConnector.StateSystem.Tests`, xUnit) is added **only** to cover `GS1CodeHelper`'s pure functions — because they are the only non-trivial new logic and have a clear contract. Everything else is verified manually end-to-end.

---

## 4. File inventory

### New files (2)

| File | Purpose |
|---|---|
| `TJConnector.StateSystem/Helpers/GS1CodeHelper.cs` | Pure static helper: `StripGroupSeparators`, `TryInsertGroupSeparator` |
| `TJConnector.StateSystem.Tests/GS1CodeHelperTests.cs` (+ `.csproj`) | xUnit tests for the helper |

### Modified files (7)

| File | Change |
|---|---|
| `TJConnector.Api/appsettings.json` (gitignored) | Add `TJConnection:EmissionCodeFormat:{Pack,Bundle}` keys |
| `TJConnector.StateSystem/Model/ExternalRequests/MarkingCode/EmissionCreateRequest.cs` | Add `int? format` with `[JsonIgnore(Condition = WhenWritingNull)]` |
| `TJConnector.StateSystem/Model/ExternalRequests/MarkingCode/ApplicationCreateRequest.cs` | Add `DateTimeOffset? productionDate` (locationUuid already present) |
| `TJConnector.Api/Services/OrderService.cs` | Inject `IConfiguration`; wire `format` in `CreateOrderAsync`; fix status switch in `GetExternalOrderByIdAsync` (remove `6 => 5`) |
| `TJConnector.Api/Transit/4 EmissionServiceConsumer.cs` | Load `Locations.FirstOrDefault`; set `productionDate` + `locationUuid`; loop packs through `TryInsertGroupSeparator`; hard-fail on malformed |
| `TJConnector.Api/Controllers/OrderController.cs` | Strip GS in `DownloadOrderContent` before `string.Join` |
| `TJConnector.Api/Controllers/BatchController.cs` | Strip GS in `DownloadBatchContent` before `string.Join` |

### Needs verification (read first during implementation, modify if required)

- `TJConnector.Api/Transit/7 ContainerAggregationConsumer.cs`
- `TJConnector.Api/Transit/9 ProcessAggregationConsumer.cs`

If either builds an outgoing request body containing pack codes sourced from `package.Content.Packs`, apply the same `TryInsertGroupSeparator` + hard-fail loop. If they only move SSCC / bundle codes (no pack codes), leave untouched.

---

## 5. Configuration

Added to `appsettings.json` under `TJConnection`:

```json
"TJConnection": {
  "BaseURL": "...",
  "Token": "...",
  "EmissionCodeFormat": {
    "Pack":   0,
    "Bundle": 1
  }
}
```

- `Pack` — integer, used as `format` in the body when `CodeOrder.Type == 0`.
- `Bundle` — integer, used as `format` in the body when `CodeOrder.Type == 1`.
- Container emission (`CodeOrder.Type == 3`) omits `format` entirely.
- `CodeOrder.Type == 2` is dead code and has no config entry.

**Read strategy:** `OrderService` gets `IConfiguration` constructor-injected and reads keys with safe defaults:

```csharp
int? format = order.Type switch
{
    0 => _config.GetValue<int>("TJConnection:EmissionCodeFormat:Pack",   0),
    1 => _config.GetValue<int>("TJConnection:EmissionCodeFormat:Bundle", 1),
    _ => null
};
```

---

## 6. `GS1CodeHelper`

```csharp
namespace TJConnector.StateSystem.Helpers;

public static class GS1CodeHelper
{
    private const char GS = '\u001d';

    // Removes every GS (0x1D) from the code. No-op on null/empty.
    public static string StripGroupSeparators(string code)
        => string.IsNullOrEmpty(code) ? code : code.Replace("\u001d", string.Empty);

    // Inserts a GS before the "93" AI in a GS1 pack code.
    // Expected input (without GS): 01<14-digit GTIN>21<8-char serial>93<rest>
    //   fixed prefix length before "93" = 2 + 14 + 2 + 8 = 26
    // Returns true on success (or idempotent pass-through if GS already present).
    // Returns false when the input doesn't match the expected shape — caller
    // must log and abort, never send.
    public static bool TryInsertGroupSeparator(string code, out string result)
    {
        result = code;
        if (string.IsNullOrEmpty(code)) return false;
        if (code.Contains(GS)) return true;
        if (code.Length < 28) return false;
        if (code[26] != '9' || code[27] != '3') return false;
        result = code.Insert(26, "\u001d");
        return true;
    }
}
```

**Properties:**
- Pure, static, no DI, no logging.
- `TryInsertGroupSeparator` is idempotent: codes that already contain GS are accepted unchanged (covers the case where the legacy SQL table ever starts providing well-formed codes).
- Defensive shape check: length ≥ 28 and `code[26..28] == "93"`. Anything else is rejected.
- Hard failure (no partial sends): caller aborts the whole package on any rejected code.

---

## 7. Per-requirement changes

### 7.1 `format` field on emission (req 1)

**DTO change — `EmissionCreateRequest.cs`:**

```csharp
[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
public int? format { get; set; }
```

**Wiring — `OrderService.CreateOrderAsync` around line 185:**

```csharp
int? format = order.Type switch
{
    0 => _config.GetValue<int>("TJConnection:EmissionCodeFormat:Pack",   0),
    1 => _config.GetValue<int>("TJConnection:EmissionCodeFormat:Bundle", 1),
    _ => null
};

var emissionRequest = new EmissionCreateRequest
{
    codesCount    = order.CodesCount,
    productUuid   = order.ProductUuid,
    markingLineUuid = order.MarkingLineUuid,
    factoryUuid   = order.FactoryUuid,
    Type          = (order.Type == 3 ? (sbyte)0 : order.Type),
    format        = format
};
```

### 7.2 Strip GS on UI download (req 2)

**`OrderController.DownloadOrderContent`:**

```csharp
var fileContent = string.Join(
    Environment.NewLine,
    content.OrderContent.Select(GS1CodeHelper.StripGroupSeparators));
return File(Encoding.UTF8.GetBytes(fileContent), "text/plain", $"codes_{id}.txt");
```

**`BatchController.DownloadBatchContent`** — same transform applied to each `orderContent.OrderContent` in the existing loop, before the `string.Join`.

Applied unconditionally. Container / pack downloads are no-ops (no GS to strip).

### 7.3 Insert GS for application / aggregation (req 3)

**`4 EmissionServiceConsumer.cs`** — replace the `groupCodes = package.Content.Select(...)` block with:

```csharp
var badCodes = new List<string>();
var groupCodes = new List<GroupCode>(package.Content.Count);

foreach (var pc in package.Content)
{
    var packs = new string[pc.Packs.Length];
    for (int i = 0; i < pc.Packs.Length; i++)
    {
        if (!GS1CodeHelper.TryInsertGroupSeparator(pc.Packs[i], out var inserted))
            badCodes.Add(pc.Packs[i]);
        packs[i] = inserted;
    }
    groupCodes.Add(new GroupCode { groupCode = pc.Bundle, codes = packs });
}

if (badCodes.Count > 0)
{
    var preview = string.Join(", ", badCodes.Take(5));
    _logger.LogError(
        $"Package {package.SSCCCode}: {badCodes.Count} malformed pack codes, cannot send to apply. Examples: {preview}");
    package.Comment = $"Malformed pack codes ({badCodes.Count}): {preview}";
    package.AddStatus(-4);
    _context.Entry(package).State = EntityState.Modified;
    await _context.SaveChangesAsync();
    return;
}
```

**Aggregation consumers** (`7`, `9`) — verify during implementation; apply the same pattern if they also ship pack codes from `package.Content.Packs`.

### 7.4 `productionDate` + `locationUuid` on application (req 4)

**DTO change — `ApplicationCreateRequest.cs`:** add `public DateTimeOffset? productionDate { get; set; }` (locationUuid already present).

**`EmissionServiceConsumer`** — before building the request, and **before** the GS insertion loop so we fail fast on missing config:

```csharp
var location = await _context.Locations.FirstOrDefaultAsync();
if (location == null)
{
    _logger.LogError($"Package {package.SSCCCode}: no location row in DB.");
    package.Comment = "No location configured";
    package.AddStatus(-4);
    _context.Entry(package).State = EntityState.Modified;
    await _context.SaveChangesAsync();
    return;
}
```

Then on the `ApplicationCreateRequest`:

```csharp
var applicationBody = new ApplicationCreateRequest
{
    applicationDate = DateTimeOffset.UtcNow.AddHours(-4),
    productionDate  = DateTimeOffset.UtcNow.AddHours(-4),
    factoryUuid     = factory.ExternalUid,
    markingLineUuid = markingLine.ExternalUid,
    locationUuid    = location.ExternalUid,
    result          = 0,
    type            = 2,
    groupCodes      = groupCodes
};
```

`productionDate` uses the same `.AddHours(-4)` offset as `applicationDate` for consistency.

### 7.5 Fix emission status success mapping (req 5)

**`OrderService.GetExternalOrderByIdAsync` — lines 67–76:**

Replace:

```csharp
localOrder.Status = externalOrder.Content.status switch
{
    0 => -2,
    1 => 2,
    3 => 3,
    4 => 4,
    5 => -3,
    6 => 5,
    _ => -4
};
```

With:

```csharp
localOrder.Status = externalOrder.Content.status switch
{
    0 => -2,
    1 => 2,
    3 => 3,
    4 => 4,   // success — ready for download (was 6 in the old external API)
    5 => -3,
    _ => -4
};
```

The `6 => 5` branch is obsolete and actively harmful: it silently marked orders Done without downloading codes. Removing it means any legacy order still returning external `6` lands in `-4` (unknown) instead of fake-Done, which is the correct failure mode.

---

## 8. Error handling

All new failure paths follow the existing consumer convention: log, set `Comment`, `AddStatus(-4)`, save, return. No exceptions bubble to MassTransit.

| Failure | Where | Status | Comment |
|---|---|---|---|
| Any malformed pack code | `EmissionServiceConsumer` | `-4` | `"Malformed pack codes (N): {preview}"` |
| No `Locations` row | `EmissionServiceConsumer` | `-4` | `"No location configured"` |
| Missing `EmissionCodeFormat:*` key | `OrderService.CreateOrderAsync` | n/a | Silent fallback to defaults (`Pack=0`, `Bundle=1`) via `GetValue<int>(key, default)` |

**Hard-fail on any bad pack code** (not a threshold). Error log and `package.Comment` include up to 5 sample bad codes.

---

## 9. Testing

### Unit tests — `TJConnector.StateSystem.Tests` (new project, xUnit)

Scope: `GS1CodeHelper` only.

| Case | Expected |
|---|---|
| Null or empty input to `StripGroupSeparators` | Returns input unchanged |
| Input with one GS | GS removed |
| Input with multiple GS | All GS removed |
| Null or empty input to `TryInsertGroupSeparator` | Returns `false`, out = input |
| Well-formed 28-char code ending in `93xxx` | Returns `true`, out has GS at index 26 |
| Sample from `bundles.txt` (already has GS) | Returns `true`, out = input (idempotent) |
| Code length < 28 | Returns `false` |
| Code length ≥ 28 but `code[26..28] != "93"` | Returns `false` |

The test project references `TJConnector.StateSystem` only; no API / DB dependencies.

### Manual verification checklist (implementation phase)

1. **format field** — create a Type 0 order, capture outgoing request to `markingCode/emission`, confirm body contains `"type": 0, "format": 0`. Repeat for Type 1 (`"type": 1, "format": 1`) and Type 3 (`container/emission`, `"type": 0`, no `format`).
2. **Download strip** — fetch a completed bundle order via UI, open file with `Format-Hex`, confirm no `0x1D` bytes.
3. **Apply with GS** — upload a `(Code, SSCC)` pair, intercept outgoing `markingCode/report/apply`, confirm pack codes have `\u001d` at index 26 and body has `productionDate` + `locationUuid`.
4. **Apply hard-fail** — inject a deliberately malformed pack row into the legacy `ContentTable`, upload, confirm package transitions to `-4`, log + `Package.Comment` contain the bad-code preview, and **nothing** was sent to external.
5. **Auto-download on status 4** — process an emission order to completion, confirm external status refresh returns `4`, internal status becomes `4`, `ProcessOrdersConsumer` case 4 fires, `CodeOrderContent` row is created, order ends at status `5`.

---

## 10. Rollout

Single commit / single deploy. No database migrations (no schema changes). No dependencies added beyond the new test project's xUnit reference.

Config change to `appsettings.json` must be applied to the deployed environment before restart. Sensible defaults (`Pack=0`, `Bundle=1`) are baked into the code via `GetValue<int>(key, default)` so an omitted config section will not break emission — it will just use those defaults.

---

## 11. Open questions flagged for implementation

1. **Aggregation consumers (`Transit/7`, `Transit/9`):** do they ship pack codes that also need GS injection? Resolved by reading the files as the first implementation step.
2. **`productionDate` timezone:** using `.AddHours(-4)` to match `applicationDate`. If the external API actually wants UTC or local Dushanbe (UTC+5), we change both in the same place.
