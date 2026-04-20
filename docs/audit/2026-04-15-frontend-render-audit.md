# TJConnector.Web Blazor Server Render Audit (2026-04-15)
## Recent Git Activity Summary

- **39cd0a8** "Claude performance" (2026-02-28) - Major refactor: Batches.razor split-pane layout, added IAsyncDisposable, toast/confirm dialogs, pagination on batchPage/orderPage fields.
- **baf36ce** "fix batches for sscc" - Minor order fix
- **79e57e5** "DANGER not equal" - Risky comparison logic change  
- **2ea0cd4** "fix batchdto and batches page" - BatchDTO structure changes
- **3616f80** "Batches. Omg" - Earlier batch page rewrite

## Candidate Render Bug Sites (Ranked by Likelihood)

### CRITICAL: CustomTable.razor - No @key on foreach loops (Lines 22, 31)
**File:** TJConnector.Web/Components/CustomTable.razor:22,31  
**Risk:** Pagination + sort changes PagedItems collection, but no @key directive. Blazor may reuse DOM elements incorrectly when CurrentPage increments/sort changes, causing stale event handlers or incorrect row selection.  
**Evidence:** Recent pagination feature added (CurrentPage, PrevPage/NextPage methods). Without @key="item.Id", Blazor cannot detect collection identity changes.  
**Expected Fix:** Add `@foreach (var item in PagedItems) @key="item.Id"`

### HIGH: Batches.razor - Missing @key on PagedBatches (Line 79)
**File:** TJConnector.Web/Pages/Batches.razor:79  
**Risk:** When batchPage changes, PagedBatches collection changes with no @key. Combined with auto-refresh loop (line 443) updating batches every 5-30s, rows may re-render incorrectly.  
**Evidence:** AutoRefreshLoop reassigns batches = await BatchService.GetBatchesAsync(). Pagination logic uses Skip().Take() creating new collection. No @key means Blazor treats rows as reusable.  
**Expected Fix:** Add `@key="b.Id"` to row element.

### HIGH: ShowToast Method - async void Anti-Pattern (Line 667)
**File:** TJConnector.Web/Pages/Batches.razor:667  
**Risk:** If component unmounts during 4-second Task.Delay, StateHasChanged on line 674 executes on disposed component. Exception silently swallowed by async void.  
**Evidence:** No safety check against component disposal. Toasts collection modified after component gone.  
**Expected Fix:** Change to `private async Task ShowToast(...)` and await callers.

### HIGH: Containers.razor - ReprocessAll Loop Unsafe (Lines 508-531)
**File:** TJConnector.Web/Pages/Containers.razor:508-531  
**Risk:** Loop calls RequestService.ReprocessPackage in fire-and-forget pattern. If DisposeAsync happens during loop, StateHasChanged calls may operate on disposed component.  
**Expected Fix:** Check `_cts.Token.IsCancellationRequested` inside loop before StateHasChanged.

### MEDIUM: AutoRefreshLoop - No IsActive Guard (Lines 443-469)
**File:** TJConnector.Web/Pages/Batches.razor:443-469  
**Risk:** Between DisposeAsync and token.IsCancellationRequested, StateHasChanged could execute on disposed component.  
**Expected Fix:** Add `private bool _isActive = true;` set to false in DisposeAsync. Guard StateHasChanged.

### MEDIUM: CustomTable - Null Comparator in Sort (Lines 88-89)
**File:** TJConnector.Web/Components/CustomTable.razor:88-89  
**Risk:** OrderBy returning null for desc sort causes unstable LINQ behavior.  
**Expected Fix:** Rewrite using if (SortAscending) ascending else descending pattern.

### MEDIUM: Status Mapping Divergence (Order.razor:446 vs BatchDTO.cs:109)
**File:** TJConnector.Web/Pages/Order.razor:442-457 vs SharedLibrary/Models/BatchDTO.cs:109-121  
**Issue:** Order.razor maps status -4 to "Archived", but BatchDTO doesn't define -4. CLAUDE.md says -4 is "Unknown external status".  
**Risk:** Inconsistent UX if API returns -4.  
**Expected Fix:** Align all mappers to single definition.

## Auto-Refresh Loops

### Batches.razor AutoRefreshLoop (Line 443-469)
- Fire-and-forget with cancellation token guard
- DisposeAsync cancels _cts at line 414
- StateHasChanged uses await InvokeAsync with no IsActive check
- Risk: Missing IsActive flag between dispose and cancellation

### Containers.razor AutoRefreshLoop (Line 394-425)
- Same pattern: Fire-and-forget, cancellation guard
- DisposeAsync at line 381
- Risk: No IsActive guard

## Service Layer Error Surface

EnsureSuccessStatusCode() at: BatchService.cs:31,38,45,56,63,70; PackageRequestService.cs:26,35,50,62

**Error path:** Click Download → DownloadBatchContentAsync() → EnsureSuccessStatusCode() throws on 404/500 → caught as "Download failed" toast with no details.

**Problem:** No distinction between 404 (deleted), 500 (server), timeout (network), 503 (service down). All render same generic toast.

**Recommendation:** Log status code and display meaningful message per error type.

## Lifecycle Issues

### Batches.razor
- OnInitializedAsync: Line 396 ✓  
- OnAfterRenderAsync: Line 403 ✓  
- DisposeAsync: Line 412 ✓ (missing IsActive flag)

### Containers.razor
- OnInitializedAsync: Line 363 ✓  
- OnAfterRenderAsync: Line 370 ✓  
- DisposeAsync: Line 379 ✓ (missing IsActive flag)

### Order.razor
- IAsyncDisposable: NOT implemented
- DisposeAsync: Missing (RISK)

## Other Findings

### Missing @key on All foreach Loops
Batches:13,79,180,246,284,302; Containers similar; Order similar.  
**Impact:** Blazor reuses DOM nodes incorrectly causing stale handlers and intermittent bugs.

### Toast Lifecycle
ShowToast runs 4 seconds. Rapid clicks create multiple toasts lingering after navigation. No queue management.

### No SignalR
Program.cs configures MaximumReceiveMessageSize=512KB but no actual wiring. Intentional per CLAUDE.md. Real-time features would silently fail.

## Summary

**Most likely render bug:** Missing @key on foreach loops, especially CustomTable and Batches pagination. When state changes, Blazor cannot identify rows correctly, causing stale DOM and incorrect event bindings.

**Secondary risks:** async void ShowToast, missing IsActive guards, null sort comparators.

**Severity:** High - affects all main pages.

**Manifestations:**
- Rows showing wrong data after pagination
- Click handlers firing on wrong row  
- Sort/filter not updating visually
- Page not rendering after navigation

**Immediate action:** Add @key directives to all foreach. Change ShowToast to async Task. Add IsActive flag.

