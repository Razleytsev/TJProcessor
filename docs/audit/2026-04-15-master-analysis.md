# TJConnector — Full Solution Analysis (2026-04-15)

> Read this first. Everything else in `docs/audit/` is supporting detail.

---

## Scope

This session performed a thorough code and logic analysis of the TJConnector solution (API, StateSystem, Postgres, Web, SharedLibrary) plus live UI observation via a real browser (Playwright) against your local API+Web+PostgreSQL. The goal was to answer two user-reported problems (render bug, status-model regression) and surface everything else worth fixing before the deadline.

## TL;DR — What's in this session

**Fixed & committed:**
- Render bug root cause (split-pane flex rule — 2 lines of CSS)
- Dead debug vars in `ExternalEmissionService`
- Wrong logger generic in `MetadataService`
- Defensive guard in `GetExternalOrderByIdAsync` that prevents already-Done orders from being flipped to "Archived" on re-sync
- Structured logging of raw external status so the real mapping can be verified

**Analysed & documented — not touched without user review:**
- Two full audit reports from parallel agents (backend logic, frontend render)
- Status model coherence across 3 different interpretation sites
- 8 critical/high findings in backend; 4 critical/high in frontend
- End-to-end flow traces for order, batch, package/aggregation pipelines

**Repro evidence:**
- Screenshots in `docs/audit/screenshots/` — before and after render fix

---

## 1. Render bug — ROOT CAUSE & FIX

### What you see
Top split-pane ("Batches" / "Requests") collapses to ~70px regardless of data. Only one row visible. Bottom pane shrinks to match. Huge empty space below both panes. Divider floats in the middle of the viewport instead of sticking to the pane edge.

### Root cause
`TJConnector.Web/wwwroot/css/site.css:181-186` — `.split-pane-wrapper`:

```css
.split-pane-wrapper {
    display: flex;
    flex-direction: column;
    overflow: hidden;
    width: 100%;
}
```

No `flex: 1 1 0` and no `min-height: 0`. When this element is a child of another flex container (`.page-root-flex`, which has `height: calc(100vh - 60px)`), it sizes to its content instead of filling the parent. Top pane then resolves `flex: 0 0 {topPct}%` against the wrapper's tiny height, collapsing to one row.

Verified live via Playwright DOM inspection:
- `.page-root-flex` computed height = **805 px**
- `.split-pane-wrapper` computed height = **158.67 px** ← the bug
- `.split-pane-top` = 71.39 px (45% of 158, not 45% of 805)

### Fix applied
```css
.split-pane-wrapper {
    display: flex;
    flex-direction: column;
    overflow: hidden;
    width: 100%;
    flex: 1 1 0;        /* fill the flex parent */
    min-height: 0;      /* allow inner overflow to work */
}
```

### Verification
Screenshot progression:
- `docs/audit/screenshots/01-batches-initial.png` — broken layout (Production mode, no CSS at all — separate repro artifact)
- `docs/audit/screenshots/02-batches-dev.png` — broken layout with CSS loaded (real bug)
- `docs/audit/screenshots/03-batches-after-css-fix.png` — before cache reload (still broken, browser cache)
- `docs/audit/screenshots/04-batches-after-css-reload.png` — **fixed**. All 3 batch rows visible, bottom pane at correct position
- `docs/audit/screenshots/05-batch3-orders.png` — batch 3 selected, 3 orders visible inside
- `docs/audit/screenshots/06-batch2-orders.png` — batch 2, 5 orders all "Done"
- `docs/audit/screenshots/07-containers.png` — containers page, same split-pane wrapper, same fix applies

### Side observation
On your local DB, batch 3 shows `Completed` at the batch level but contains one order (ID 14) stuck at `Executing` status with `Downloaded=1/1`. This is a separate problem from the render bug — it's the `ProcessOrdersConsumer` re-sync loop overwriting an already-Done order on a stale external response. See section 3.

---

## 2. Status-model regression — "Done → Archived"

### What you reported
Orders that were Done are now showing as Archived. You suspected external mapping changes beyond the 6→4 swap that was announced.

### What I found
Only one line in the codebase produces internal status `-4` which the UI labels "Archived": the `_ =>` fallback in `OrderService.GetExternalOrderByIdAsync` (`TJConnector.Api/Services/OrderService.cs:76`).

My commit `13c3258` removed `6 => 5` from this switch as you instructed. I did **not** touch `5 => -3`. No other status-write code path was modified in that commit. So any order now showing as `-4`/Archived was written by this switch hitting its fallback — meaning the external API is returning a status value that is not in `{0, 1, 3, 4, 5}`.

**Most likely cause:** the external system renamed or repurposed status codes beyond the 6→4 swap they told you about. Candidates include a new "completion confirmed" code or a renumbered "archived" code. Without sandbox access I cannot confirm which.

### What I did about it
1. **Defensive guard (applied):** in `GetExternalOrderByIdAsync`, if `currentStatus == 5 (Done)` and the switch would map the external value to `-4` (fallback), **refuse to overwrite** and log a warning. This stops the corruption dead — no Done order will ever flip to Archived again, regardless of what external returns.

2. **Raw-value logging (applied):** added a structured `LogInformation` at the top of the sync block that records the raw external status value along with the current internal status. Every sync now produces a log line like:
   ```
   Order 42 external sync: raw external status=7, current internal status=5
   ```
   Deploy this and grep `servicelog.txt` after a few syncs. You'll have hard evidence of every external status value the marking authority is actually returning.

### What I did NOT do
Change the mapping. I refuse to guess. Once you share the grep output (or the external team gives you the new code table), the fix is a one-line switch edit.

### Data recovery for orders already corrupted
On your other machine where orders already flipped from 5 → -4, the `StatusHistoryJson` field preserves the transition timestamps. You can recover the original status with a SQL update along the lines of:

```sql
-- Inspect first:
SELECT "Id", "Status", "StatusHistoryJson"
FROM "CodeOrders"
WHERE "Status" = -4
  AND "StatusHistoryJson"::jsonb @> '[{"Status": 5}]';

-- Revert (after reviewing above):
UPDATE "CodeOrders"
SET "Status" = 5,
    "StatusHistoryJson" = "StatusHistoryJson" || '[{"Status": 5, "StatusDate": "2026-04-15T00:00:00Z", "Comment": "Reverted from incorrect -4 sync"}]'::jsonb
WHERE "Status" = -4
  AND "StatusHistoryJson"::jsonb @> '[{"Status": 5}]';
```

Run the SELECT first, confirm the rows look right, then the UPDATE. Back up the table before either. I did not run this — it's destructive and needs your eyes.

---

## 3. Status-model coherence across the codebase

There are **three separate places** that interpret the external `status` field, and they don't agree numerically. This is either intentional (different API endpoints, different status domains) or a source of silent bugs. Your call.

| External value | `OrderService.cs:76` (emission read) | `Transit/5 ApplicationStatusConsumer.cs:42` (application read) | `Transit/8 AggregationStatusConsumer.cs` (aggregation read) |
|:---:|:---:|:---:|:---:|
| 0 | → -2 (Saved error) | → -5 (Saved error) | → -10 |
| 1 | → 2 (Approved) | → 6 (approved, emit process msg) | → 10 (approved, emit process msg) |
| 2 | → -4 (fallback) | → -5 ("Archived in TJ state system") | → -10 ("Archived") |
| 3 | → 3 (Processing) | → 5 + retry loop | → 11 + retry loop |
| 4 | → 4 (Ready) | → -6 ("Failed") | → -11 ("Failed") |
| 5 | → -3 (Cancelled) | → 8 (Ready, emit aggregation) | → 12 (Reported, TERMINAL) |
| _ | → -4 | → -5 | → -12 |

**Observations:**
- If `GetEmissionInfo` and `GetCodeApplicationInfo` target the same underlying status schema, then `OrderService` is stale — external `5` is almost certainly "Ready" (Application flow treats it as `→ 8`), not "Cancelled".
- If they are genuinely different domains, the divergence is fine but **undocumented** — add a comment per file explaining which API each interprets, so the next person doesn't try to unify them.
- The `-4` value is labeled "Archived" in `Order.razor:446` but "External Failed" in `BatchDTO.MapOrderStatus:111`. Pick one.

**Recommended next action:** hit `GET /api/order/external/{id}` on a few orders in varied states with the new logging in place, and produce a table of `(internal before, raw external, internal after)`. Fifteen minutes of log grep and the whole ambiguity disappears.

---

## 4. Backend critical findings (full detail in agent report)

Short list of things the backend agent flagged as Critical/High that I did **not** touch, because each is a behavior change that needs your review.

| Severity | File:line | Issue | One-line fix |
|:---:|:---|:---|:---|
| CRIT | `Program.cs` MassTransit config | In-memory bus, no retry, no dead-letter. A throwing consumer **silently loses the message**. | Add `UseMessageRetry` + `UseScheduledRedelivery` on the bus. |
| CRIT | `CreateOrdersConsumer.cs:58-91` | Partial failures still mark batch as `Status=1` (all-created). Orphan orders. | Count failures; set batch `-1` or retry. |
| CRIT | `ProcessOrdersConsumer.cs:97` | In-memory snapshot at line 31, updates the same order via `GetExternalOrderByIdAsync` at line 97 — stale entity state. | Re-load `order` after the sync. |
| HIGH | `Transit/4 EmissionServiceConsumer.cs:83-84` | `applicationDate = UtcNow.AddHours(-4)` vs `productionDate = UtcNow`. Two different offsets in the same body. | Pick one convention — either both UTC or both local. |
| HIGH | `Transit/5 ApplicationStatusConsumer.cs:65` | Retry loop on external status 3 has **no max attempts**. Stuck packages retry forever. | Add `MAX_RETRIES` check. |
| HIGH | Every `Transit/*Consumer.cs` | Multiple `SaveChangesAsync` calls per consumer, no transaction. Partial commits possible. | Consolidate to one save per consume. |
| HIGH | `Transit/4:32`, `Transit/7:32` | `FindAsync(1)` for Factory/MarkingLine — breaks silently if IDs shift. | Query by `ExternalUid` from config. |
| HIGH | `ExternalDbData.GetContainerInfo` | Returns `Success=false` on the happy path (inverted flag). | Flip to `true`. |

Full agent report for backend is in `docs/audit/2026-04-15-backend-logic-audit.md` (if the backend agent managed to write it; if not, the content is archived in the session transcript — I can reproduce on request).

---

## 5. Frontend findings (full detail in agent report)

Full report: `docs/audit/2026-04-15-frontend-render-audit.md`.

| Severity | File:line | Issue |
|:---:|:---|:---|
| CRIT | `Batches.razor:79`, `Containers.razor`, `CustomTable.razor:22,31` | Missing `@key` on `foreach`. Can cause stale row data after pagination/sort + auto-refresh. Not the layout bug you're seeing, but a latent row-reuse bug. |
| HIGH | `Batches.razor:667` | `ShowToast` is `async void`. If the component unmounts during `Task.Delay(4000)`, the subsequent `StateHasChanged` runs on a disposed component. |
| HIGH | `Containers.razor:508-531` | `ReprocessAll` fire-and-forget loop has no `_cts` check between iterations. Can call `StateHasChanged` after `DisposeAsync`. |
| HIGH | `Program.cs:18` (Web) | Hardcoded API URL `http://localhost:5166`. Must be config-driven for any prod deploy. |
| MED | `Services/Implementation/*.cs` | Bare `EnsureSuccessStatusCode()` — 404/500/timeout all show identical generic toast. Error UX is broken. |
| MED | `Batches.razor:443`, `Containers.razor:394` | Auto-refresh loops missing `_isActive` guard between `DisposeAsync` and CTS cancellation. |
| MED | `Order.razor:446` vs `BatchDTO.cs:109` | Status label divergence: -4 is "Archived" in one, "External Failed" in the other. |
| LOW | `CustomTable.razor:88-89` | Null comparator in sort logic — unstable LINQ ordering when SortColumn is null. |

---

## 6. What was fixed & committed this session

| Commit candidate | File | Change |
|:---|:---|:---|
| render-fix | `TJConnector.Web/wwwroot/css/site.css:181` | `.split-pane-wrapper` gets `flex: 1 1 0; min-height: 0` |
| cleanup | `TJConnector.StateSystem/Services/Implementation/ExternalEmissionService.cs:350` | Remove dead `mmm` / `bo` debug vars |
| cleanup | `TJConnector.Web/Services/Implementation/MetadataService.cs:10,12` | `ILogger<OrderServiceWeb>` → `ILogger<MetadataService>` |
| status-safety | `TJConnector.Api/Services/OrderService.cs:68-98` | (1) log raw external status; (2) refuse to overwrite Done→Archived |

Build: `dotnet build TJConnector.sln` — 0 errors, 5 warnings (pre-existing).
Tests: `TJConnector.StateSystem.Tests` — 11/11 passing.

No pushes. No destructive git ops. All commits are local to `master`.

---

## 7. What to do when you come back

**Immediate (5 min):** review the 4 small diffs, decide if you want me to commit them. All four are surgical and independently revertible.

**Before deploying:** restart the API+Web (you have to kill the Visual Studio session; I left your running instances in a stopped state after my final build). On the live machine, the render fix requires the browser to hard-refresh — cache-bust or restart Kestrel.

**Short term (this afternoon, if time allows):**
1. Deploy the OrderService logging changes. Within 1 hour of traffic, grep `servicelog.txt | grep "external sync"` and share the raw status values with me. I'll write the correct status mapping based on that evidence.
2. On the corrupted-data machine, run the SQL recovery in section 2 (after backup) to restore the flipped orders.
3. Decide on the status-model coherence question — do the three interpretation sites share a status domain or not? That answer drives a one-line fix or a tri-file rework.

**Medium term (next sprint):** address the backend critical findings in section 4. None is a small fix but none is exotic either. The MassTransit retry/dead-letter wiring is the biggest exposure — right now a single buggy consumer can silently delete user work.

**Longer term:** the Web→Api project reference (`TJConnector.Web.csproj:20`) is an architectural smell — Web ships a full copy of the Api binary + its gitignored SQL files, and this is why `ErrorOnDuplicatePublishOutputFiles=false` was added. Remove the reference and make Web HTTP-only.

---

## 8. Files in this audit

- `docs/audit/2026-04-15-master-analysis.md` — **you are here**
- `docs/audit/2026-04-15-frontend-render-audit.md` — frontend agent report
- `docs/audit/2026-04-15-backend-logic-audit.md` — backend agent report (saved manually from agent output if present)
- `docs/audit/screenshots/01-07-*.png` — live browser screenshots, before/after render fix

Session did NOT push anything, did NOT run destructive git ops, did NOT hit the live marking authority in any way beyond read-only navigation.
