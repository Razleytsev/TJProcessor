# TJConnector Backend Logic Audit Report (2026-04-15)

Archived from parallel audit agent. Agent ran in read-only mode and returned findings as text; this file preserves them verbatim.

---

## Executive summary

Deep analysis of TJConnector backend reveals **3 CRITICAL issues** causing data loss and state corruption, **5 HIGH-severity findings** with data integrity risks, and multiple medium/low findings affecting reliability. The system has strong async/await patterns but lacks transaction boundaries, message persistence, and error recovery.

## Flow traces

### 1. Single Order Flow (Type 0,1 = Pack/Bundle; Type 3 = Container)

**CreateOrder Flow:**
- `OrderController.CreateOrder` → `OrderService.CreateOrderAsync` (line 137-227)
- Create `CodeOrder` Status=0, save to DB
- Delay 500ms (intentional — ensures DB commit before external call)
- Call `CreateCodeEmission` or `CreateContainerEmission` (Type==3 dispatch at line 202-204)
- Success: Status→1, store ExternalGuid, SaveChangesAsync
- Failure: Status→-1, store error in StatusMessage, SaveChangesAsync

**Status Sync Flow:**
- `OrderController.GetExternalOrderById` → `GetExternalOrderByIdAsync` (line 56-87)
- Fetch current order, verify ExternalGuid exists
- Call external API (line 64-66)
- Map external status at line 70-78:
  - ext 0 → -2 (Not Approved)
  - ext 1 → 2 (Approved)
  - ext 3 → 3 (Processing)
  - ext 4 → 4 (Ready)
  - ext 5 → -3 (Cancelled)
  - ext * → **-4 (Unknown/fallback)**
- Append history, SaveChangesAsync

**Processing & Download:**
- `ProcessCodeEmissionAsync` (line 89-109): Status 1 or 2 → 3
- `GetCodesFromOrderAsync` (line 111-135): Status 4 or 5 → fetch codes, insert `CodeOrderContent`
- `DownloadOrderContentAsync` (line 229-242): fetch content, update DownloadHistory, SaveChangesAsync

### 2. Batch Flow

**Creation:**
- `BatchController.CreateBatch` (line 154-188): Status=0, save, publish ProcessBatch

**BatchInitialConsumer (line 23-62):**
```
Status 0: if orders.Count==0 → publish CreateOrdersForBatch; else skip
Status 1: delay 5s, publish ProcessOrdersForBatch
Status 2: done
Status -1: cancelled
```

**CreateOrdersConsumer (line 26-92):**
- Split batch.Count into chunks ≤10,000
- For each: `CreateOrderAsync` (with 2s delay between)
- **NO exception handling on line 60** — if order 2 fails, loop exits
- Status→1, SaveChangesAsync (line 87)
- Republish ProcessBatch (line 91)

**ProcessOrdersConsumer (line 26-117):**
- Fetch orders where 0 ≤ Status ≤ 4 (line 31)
- For each order:
  - Status 2: ProcessCodeEmissionAsync, Status→3
  - Status 4: Check CodeOrderContent; if missing, GetCodesFromOrderAsync; Status→5
  - Status 0,1,3: GetExternalOrderByIdAsync (sync external)
- SaveChangesAsync per order (line 60, 76, 89)
- Delay 15s, republish ProcessOrdersForBatch (line 116)

### 3. Package/Application Flow (Container Lifecycle)

**PackageRequest Creation:**
- `PackageRequestController.CreateOrder` (line 52-107)
- Create `PackageRequest` (Status=0), save
- Create N `Package` records (Status=0), save
- Publish StateCheckSSCCBody1 (line 104)

**Transit Sequence (Consumers 1-9):**
```
1. StateCheckSSCC (Transit/1):
   Status -1 → 1 (found) or -1 (not found/bad status)
   → publish ExternalDbBody2

2. ExternalDbCheck (Transit/2):
   Status -2 → 2 (exists in ext DB)
   → publish ExternalDbContentBody3

3. ExternalDbContent (Transit/3):
   Status -3 → 3 (content loaded)
   → publish StateCreateApplicationBody4

4. StateCreateApplication (Transit/4):
   Status -4 → 4 (application created)
   → publish StateApplicationStatusBody5

5. StateApplicationStatus (Transit/5):
   Ext Status 0 → -5 (error)
   Ext Status 1 → 6 (approved) → publish StateApplicationProcessBody6
   Ext Status 2 → -5 (archived)
   Ext Status 3 → 5 (processing) + retry with exponential backoff
   Ext Status 4 → -6 (failed)
   Ext Status 5 → 8 (ready) → publish StateCreateAggregationBody7
   Ext * → -5 (unknown fallback)

6. StateProcessApplication (Transit/6):
   Status 6 → 7 (processing) or -7 (failed)
   → publish StateApplicationStatusBody5 (re-check)

7. StateCreateAggregation (Transit/7):
   Status -8 → 8 (aggregation created)
   → publish StateAggregationStatusBody8

8. StateAggregationStatus (Transit/8):
   Ext Status 0 → -10 (error)
   Ext Status 1 → 10 (approved) → publish StateProcessAggregationBody9
   Ext Status 2 → -10 (archived)
   Ext Status 3 → 11 (processing) + retry
   Ext Status 4 → -11 (failed)
   Ext Status 5 → 12 (reported) — TERMINAL
   Ext * → -12 (unknown fallback)

9. StateProcessAggregation (Transit/9):
   Status 10 → 9 (processing) or -9 (failed)
   → publish StateAggregationStatusBody8 (re-check)
```

## Status model coherence matrix

| Scenario | External API Status | OrderService Line 70-78 | ApplicationStatusConsumer Line 42-92 | Notes |
|----------|------|------|------|------|
| Not Approved | 0 | → -2 | → -5 | DIVERGENT |
| Approved | 1 | → 2 | → 6 | Same intent, different numeric range |
| Processing | 3 | → 3 | → 5 (retry mode) | Same intent |
| Ready | 4 | → 4 | — | Only in OrderService |
| Cancelled/Archived | 5 | → -3 | → -5 | DIVERGENT |
| Unknown | * | → -4 | → -5 | Both fallback, different values |

KEY FINDING: OrderService and ApplicationStatusConsumer operate on different entity types with different external APIs:
- OrderService (CodeOrder): Emission API, narrow status range (-4 to 5)
- ApplicationStatusConsumer (Package): Container API, wide status range (-12 to 12)

This is likely correct domain separation — but the numeric ranges don't overlap, so a mix-up would be immediately obvious. Document it per file.

## CRITICAL findings

### CRITICAL-1: In-Memory Message Bus with No Dead-Letter or Retry

**Location:** `TJConnector.Api/Program.cs:60-81`

```csharp
cfg.UsingInMemory((context, config) => {
    config.ConfigureEndpoints(context);
});
```

- MassTransit uses `UsingInMemory()` — no message persistence
- If any consumer throws an exception, the message is LOST immediately
- No automatic retry, no poison message queue, no dead-letter
- Example: `ProcessOrdersConsumer` throws on line 105 → all 5 orders stuck forever

**Suggested Fix:**
```csharp
cfg.UsingInMemory((context, config) => {
    config.RetryConfiguration.SetRetryPolicy(cfg => cfg.Incremental(3,
        TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(1)));
    config.UseScheduledRedelivery(r => r.Interval(3, TimeSpan.FromSeconds(60)));
    config.ConfigureEndpoints(context);
});
```

### CRITICAL-2: CreateOrdersConsumer Fails Silently on Partial Order Creation

**Location:** `TJConnector.Api/TransitBatches/CreateOrdersConsumer.cs:58-91`

- If `CreateOrderAsync` throws on order 2 of 5, the loop exits
- Line 84 unconditionally sets `batch.Status = 1` (all created)
- Line 91 republishes `ProcessBatch` → BatchInitialConsumer sees Status==1
- Result: 1-2 orders exist, but batch marked complete → orphaned orders

**Suggested Fix:** Track failureCount; set batch.Status = -1 if any failed.

### CRITICAL-3: Race Condition in ProcessOrdersConsumer with Stale Order Data

**Location:** `TJConnector.Api/TransitBatches/ProcessOrdersConsumer.cs:26-117`

- Line 31: `processingOrders` is in-memory snapshot
- Line 97: `GetExternalOrderByIdAsync` loads a fresh entity and updates it in DB
- Line 98: compares `refreshed.Status` (updated) vs `order.Status` (stale)
- If another consumer updated the order between line 31 and 97, `order` is outdated

**Suggested Fix:** Re-load `order = await _context.CodeOrders.FindAsync(order.Id)` after the sync.

## HIGH findings

### HIGH-1: Timezone Mismatch in Application Creation

**Location:** `TJConnector.Api/Transit/4 EmissionServiceConsumer.cs:83-86`

```csharp
var applicationBody = new ApplicationCreateRequest {
    applicationDate = DateTimeOffset.UtcNow.AddHours(-4),  // -4 from UTC
    productionDate = DateTimeOffset.UtcNow,                 // UTC
    // ...
};
```

Two different offsets in the same body. No comment explaining why -4.

### HIGH-2: Infinite Retry Loop with Unbounded Backoff

**Location:** `TJConnector.Api/Transit/5 ApplicationStatusConsumer.cs:65-77` and `Transit/8 AggregationStatusConsumer.cs:61-73`

Retry count increments without bound. If processing never completes, the package is stuck in retry forever. Add MAX_RETRIES.

### HIGH-3: Multiple SaveChangesAsync Calls Create Partial-Commit Hazard

**Location (example):** `TJConnector.Api/Transit/1 ContainerStatusConsumer.cs:24-84`

Multiple SaveChangesAsync calls in the same consumer. If save 2 succeeds but save 3 fails, package has conflicting history. Wrap in a single transaction or consolidate saves.

### HIGH-4: Hardcoded Metadata IDs Break on Different Deployments

**Location:** `Transit/4 EmissionServiceConsumer.cs:32-34`, `Transit/7 ContainerAggregationConsumer.cs:32`

```csharp
var factory = await _context.Factories.FindAsync(1);
var markingLine = await _context.MarkingLines.FindAsync(1);
var location = await _context.Locations.FirstOrDefaultAsync();
```

In a fresh DB, default inserts may have different IDs. Move to configuration (ExternalUid).

### HIGH-5: Order Status Can Drift from 5 (Done) to -4 (Archived) on Re-sync

**Location:** `TJConnector.Api/Services/OrderService.cs:70-78` + `TJConnector.Web/Pages/Order.razor:446`

Order completes with Status 5 (Done). `ProcessOrdersConsumer` calls `GetExternalOrderByIdAsync` again at line 97 (periodic re-sync). If external API returns an unexpected status value, it maps to -4 → UI displays "Archived" even though the order was completed.

**This is the user-reported Done→Archived symptom.** Fixed defensively in main analysis report; permanent fix needs external status table.

## MEDIUM findings

### MEDIUM-1: StatusHistoryJson Append Uses Three Different Patterns
Three patterns for the same operation across the codebase. Maintainability risk, no logic bug.

### MEDIUM-2: Missing StatusHistory Append in Error Paths
`Transit/2 ExternalDbStatusConsumer.cs:26-69` writes `package.Status = -2` without `AddStatus()`. Audit trail gap.

### MEDIUM-3: No ILogger in ProcessAggregationConsumer
`Transit/9 ProcessAggregationConsumer.cs:15-19` — no `ILogger<T>` injected. Observability gap.

## LOW / NIT

- **LOW-1:** Dead code — SignalR hub and ResponseCompression commented out in `Program.cs:83-88`. Retry-fix comments in `Transit/6:31-39`, `Transit/9:28-36`.
- **LOW-2:** Inconsistent delay hardcodes: Transit/1 500ms, CreateOrdersConsumer 2000ms, ProcessOrdersConsumer 1000/15000ms, Transit/4,7 1000ms. No documented rationale.

## Open questions for the user

1. **External Status Domains:** Are marking code emission statuses (0-5) and container application statuses (0-5, different semantic) returned by the same API or different endpoints? Document mapping.
2. **Timezone Intent:** Is -4 hours in Transit/4:85 correct? Tajikistan is UTC+5, not UTC-4.
3. **Batch Partial Failure:** Desired behavior if CreateOrdersConsumer fails on order 3 of 5 — retry, mark partial, or idempotency token?
4. **Max Retries:** What's the cap for the Status==3 retry loop? Currently infinite.
5. **Entity Unification:** Should Package and CodeOrder merge, or are they intentionally separate domains?

## Priority remediation order

1. CRITICAL-1: MassTransit retry + dead-letter (prevents silent message loss)
2. CRITICAL-2: CreateOrdersConsumer exception handling + batch failure marker
3. CRITICAL-3: ProcessOrdersConsumer stale-entity re-load
4. HIGH-1: Resolve timezone policy
5. HIGH-2: MAX_RETRIES on exponential backoff
6. HIGH-3: Single SaveChangesAsync per consumer
7. HIGH-4: Metadata IDs from config
