# UX Heuristic Audit — TJConnector.Web — 2026-05-27

Pre-polish baseline: `docs/frontend-audit/2026-05-27-baseline-screenshots/*.png`
Visual pass: `docs/frontend-audit/2026-05-27-visual-pass/*.png`
Method: real-flow walkthrough in Playwright MCP (Chromium, 1440×900), interaction with mouse + keyboard, cross-referenced against the .razor sources.

## Summary

| Severity | Count |
|---|---|
| Critical | 6 |
| Major    | 11 |
| Minor    | 9 |
| **Total**| **26** |

The cross-cutting theme is *two parallel UI eras*. `/batches`, `/containers`, `/test-run` use the new split-pane + custom-modal system; `/products` and `/emission` still use the old `.container` layout with Bootstrap-default modals and the legacy `CustomTable.razor` component. Most findings either flow from that inconsistency or from missing keyboard / screen-reader affordances that a previous era of the code never installed.

Inline-fix candidates are CSS or single-file Razor edits that don't change information architecture. Deferred items need user judgment.

---

## Critical findings

### F1. Escape does not close modals
- **Pages / flows:** `/batches` → `+ Order Codes`; `/containers` → `+ Submit Containers`; `/containers` → click any package row (detail modal)
- **Heuristic:** Nielsen #3 (user control & freedom) + convention
- **Symptom:** Pressing **Esc** with a modal open does nothing. The user must mouse to the small `×` glyph or to the "Cancel" button. The e2e suite confirms this in three tests.
- **Why this matters:** Modal dismissal via Esc is the most-universally-expected keyboard affordance in modern web UIs. Power users assume it; accessibility tooling assumes it.
- **Proposed fix:** Add a single `@onkeydown:stoppropagation @onkeydown="OnModalKey"` handler on `.modal-overlay` in each of the three modals; in `OnModalKey`, close on `Escape`. Or wrap each modal's overlay in a tiny `<Modal>` component that owns this once.
- **Effort:** S (3 small Razor edits) — INLINE FIX

### F2. Form labels not associated with inputs (`for=` / `id=`)
- **Page / flow:** `+ Order Codes` modal, `+ Submit Containers` modal, `/test-run` form, `/emission` modal
- **Heuristic:** WCAG 1.3.1 + Nielsen #6 (recognition over recall)
- **Symptom:** Labels are styled but not programmatically tied to their inputs. Screen readers don't announce them. The e2e test `getByLabel('Codes Count')` fails for this reason.
- **Why this matters:** Operators using screen-reader software (audit-required in many enterprise contexts) cannot identify which field they're in. Clicking a label also does not focus its input.
- **Proposed fix:** Replace each `<label class="form-label-sm">X</label>` + bare `<input>` pair with either matching `for="X-id"` / `id="X-id"` attributes, or wrap the label around the input. Generate ids from the field name.
- **Effort:** S — INLINE FIX

### F3. Brand "Abdulla" is not a home link
- **Page / flow:** every page; click the brand at top-left of the sidebar
- **Heuristic:** Don Norman (affordance) + web convention
- **Symptom:** `<a class="navbar-brand" href="">…</a>` resolves to the current page, not `/`. Clicking the logo from `/products` does navigate (because `""` resolves to base) but it ALSO redirects to `/batches` via the Program.cs middleware — so the convention *accidentally* works for now. But `href=""` is brittle and means a Tab+Enter from another page does not produce the expected behaviour. The accessibility test for keyboard-activatable logo passes only by luck.
- **Why this matters:** The logo is the universal "take me home" affordance. It should be explicit.
- **Proposed fix:** Change `href=""` to `href="/"` (or `href="batches"` for clarity); add an `aria-label="Go to home"` or descriptive `title`.
- **Effort:** S — INLINE FIX

### F4. Required-field asterisk styled as an aggressive red pill
- **Page / flow:** `+ Order Codes` modal → "Codes Count *"
- **Heuristic:** Nielsen #4 (consistency) + Norman (signifiers)
- **Symptom:** The legacy `.text-danger` global rule converts the asterisk into a white-on-red pill that looks identical to a "ERROR" inline badge. Users perceive this as an error before they've typed anything.
- **Why this matters:** False-positive error signalling damages trust in real error states.
- **Proposed fix:** Stop the `.text-danger` rule from applying to inline asterisks in form labels. Either rename the marker to a dedicated `.form-required` span, or scope `.text-danger` to only apply where there is a sibling `.alert`. Easiest: in the .razor, replace `<span class="text-danger">*</span>` with `<span class="required-mark" aria-hidden="true">*</span>` and add a `.required-mark { color: var(--st-error-fg); font-weight: 700; }` rule.
- **Effort:** S (one Razor edit + one CSS rule) — INLINE FIX

### F5. Not-found page is a single line of unhelpful text
- **Page / flow:** any unknown route, e.g. `/this-does-not-exist`
- **Heuristic:** Nielsen #9 (help users diagnose / recover) + #10 (help)
- **Symptom:** `App.razor` renders `<p role="alert">Sorry, there's nothing at this address.</p>`. No link home, no header, no styling.
- **Why this matters:** Direct-URL access by a bookmark or shared link is common in admin tools. Landing on a near-blank page suggests the app is broken.
- **Proposed fix:** Replace the `<p>` with a small page that renders an icon glyph, an H1 "Page not found", a one-line explanation, and a button-link back to `/batches`. CSS classes for this already exist (`.empty-state`, `.btn-primary`).
- **Effort:** S — INLINE FIX

### F6. Icon-only buttons have no accessible name
- **Page / flow:** `/batches` action column (`⬇`); `/containers` action column (`↺`); modal close `×`
- **Heuristic:** WCAG 4.1.2 (name, role, value) + Nielsen #6
- **Symptom:** Buttons like `<button class="btn-xs">⬇</button>` carry no `aria-label` or `title`. Screen readers announce them as "button" with no purpose. The IBM Plex font also renders `⬇` (U+2B07) as a substitute glyph that looks like "I" — so even sighted users can't read it now.
- **Why this matters:** Both keyboard and screen-reader users are blocked from understanding action affordances.
- **Proposed fix:** Add `aria-label="Download"` / `aria-label="Reprocess"` / `aria-label="Close"` and `title` for sighted-tooltip parity. Swap `⬇` (U+2B07) for `↓` (U+2193) and `↺` (U+21BA) for `↻` (U+21BB) — both are in the IBM Plex glyph set.
- **Effort:** S — INLINE FIX

---

## Major findings

### F7. `/products` page heading uses `<h3>`, other pages use `.page-header h5`
- **Heuristic:** Nielsen #4 (consistency)
- **Symptom:** `/batches`, `/containers`, `/test-run` use a `.page-header` bar with an `<h5>` (visually rendered at 18px). `/products` and `/emission` use a bare `<h3>` outside any header bar.
- **Proposed fix:** Wrap `/products` and `/emission` headings in the same `.page-header` markup so the chrome lines up across pages.
- **Effort:** S — INLINE FIX

### F8. `/products` renders no `<table>` when empty
- **Heuristic:** Nielsen #1 (system status) + WCAG semantic-structure
- **Symptom:** `CustomTable.razor` renders the filter input but only outputs a `<table>` element after `Items` is non-empty. The e2e test fails because the column headers are tied to the rendered table.
- **Proposed fix:** Move the `<table>`+`<thead>` outside the `@if (PagedItems.Any())` branch so headers always render, with a `<tr><td colspan="@Columns.Count">No items</td></tr>` body row when empty. (`CustomTable` partially does this already, but the `<table>` itself is unconditionally there — the real bug is that on first load `Items` is `null`, not just empty, and `Items.Any()` throws or short-circuits the whole table.)
- **Effort:** S — INLINE FIX

### F9. `/emission` page is a vestigial second UI for orders
- **Heuristic:** Nielsen #4 (consistency) + #8 (minimalist design)
- **Symptom:** The "External Orders" page at `/emission` uses obsolete Bootstrap classes (`form-group`, `.close`, `modal-dialog`), an older layout (`.container` flex with hard-coded widths), and duplicated business logic for create-order that `/batches` already handles better.
- **Proposed fix:** Decide whether `/emission` is still in active use. If yes, port it to the new chrome (`.page-root-flex`, `.split-pane`, custom modal). If no, remove the page and its route. Either way, link from `/batches` if there's still a need to view orders independently of their parent batch.
- **Effort:** L — DEFERRED (info-architecture decision needed)

### F10. Browser back loses selected row + split-pane size + scroll position
- **Heuristic:** Nielsen #3 (user control & freedom)
- **Symptom:** On `/containers`, scroll the requests list, select a request, switch to `/products`, then browser-back to `/containers`. The list scrolls back to top, no row is selected, the split-pane percentage resets to default.
- **Proposed fix:** Encode the selected id and (optionally) the split-pane percentage in the URL (e.g. `?req=42&top=55`). On page mount, read them. This is the canonical "URLs encode filter state" pattern.
- **Effort:** M — DEFERRED (worth doing but touches data flow)

### F11. URL doesn't encode any filter / list state
- **Heuristic:** convention; Nielsen #3
- **Symptom:** Pagination, sort column, sort direction, filter text — all in memory only. Page refresh = back to default.
- **Proposed fix:** Adopt a query-string pattern for `batchPage`, `orderPage`, `pkgPage`, `reqPage`, `filterText`, `sortCol`, `sortDir`. Same approach as F10.
- **Effort:** M — DEFERRED

### F12. Modals lack `role="dialog"` + `aria-modal="true"` + `aria-labelledby`
- **Heuristic:** WCAG 4.1.2 + Nielsen #4
- **Symptom:** `.modal-dialog-box` is a plain `<div>`. Screen readers do not announce it as a dialog, do not announce its title, do not constrain focus inside it.
- **Proposed fix:** Add `role="dialog" aria-modal="true" aria-labelledby="modal-title-X"` to each modal box (3 modals + 1 confirm box), and add `id="modal-title-X"` to the title span.
- **Effort:** S — INLINE FIX

### F13. Focus is not trapped inside modals; closing does not restore focus
- **Heuristic:** WCAG 2.4.3 (focus order) + 2.1.2 (no keyboard trap inverted — we WANT a trap)
- **Symptom:** Tab inside an open modal continues into the background page. Closing the modal does not return focus to the button that opened it.
- **Proposed fix:** Wrap each modal in a `<FocusTrap>` helper (small Blazor component) that:
  - on open, stashes `document.activeElement` and moves focus to the modal's first focusable child;
  - cycles Tab + Shift-Tab among the modal's focusable descendants;
  - on close, restores focus.
- **Effort:** M — DEFERRED (one small new shared component + integration in 3 modals)

### F14. Sortable column hint without sort behaviour (Batches / Containers tables)
- **Heuristic:** Don Norman (signifier mismatch) + Nielsen #4
- **Symptom:** Both tables apply `cursor: pointer` to every `<th>` and the visual treatment looks identical to the genuinely sortable `CustomTable`. But there is no click handler — nothing happens when a user clicks a header. The cursor hint is a false signifier.
- **Proposed fix:** Either implement column sort (matches the `CustomTable` pattern), or remove the `cursor: pointer` on those `<th>`s. Lowest-effort win: remove the pointer cursor and any visual hover state on non-sortable headers.
- **Effort:** S (remove cursor) — INLINE FIX; OR M (implement sort) — DEFERRED

### F15. `CustomTable` filter input has no label, just placeholder text
- **Heuristic:** WCAG 3.3.2 (labels & instructions) + Nielsen #6
- **Symptom:** `<input id="filter" placeholder="Filter">` — placeholder text disappears on focus; no label is announced. Also, `id="filter"` is hardcoded — if two `CustomTable` instances were ever on one page they'd collide.
- **Proposed fix:** Add `<label class="visually-hidden" for="filter-{InstanceId}">Filter rows</label>` and a unique id; use a small magnifier icon + the visible-on-focus state for the input.
- **Effort:** S — INLINE FIX

### F16. Reprocess actions destructive-ish but no confirm dialog
- **Heuristic:** Nielsen #5 (error prevention)
- **Symptom:** "↺ Reprocess All Errors (N)" on `/containers` triggers immediately. So does the per-row `↺` button. The "Reprocess Created (N)" button also fires immediately. There's a reprocess-bar showing progress, but no chance to cancel.
- **Proposed fix:** Add a confirm dialog (re-use the existing `.confirm-box`) for "Reprocess All Errors" (the high-N case) and "Reprocess Created". Per-row single re-processing is mild enough that an inline confirm is overkill, but a toast that says "Click again within 3s to confirm" is a nice middle ground.
- **Effort:** S (confirm for the bulk actions only) — INLINE FIX (partial)

### F17. Auto-refresh indicator "↻ 30 s" is opaque
- **Heuristic:** Nielsen #1 (system status)
- **Symptom:** The header shows `↻ 30 s` — but is that "refreshes every 30s" or "30 seconds since last refresh"? When does it next tick? The pulsing-dot pattern used on `processing` badges would communicate "currently refreshing" much better.
- **Proposed fix:** Tooltip on the indicator: "Auto-refreshing every 30s". Add a tiny progress ring or animated underline that empties as the timer counts down. Or simpler: show a stopwatch dot that pulses while `isRefreshing == true`.
- **Effort:** S (tooltip) to M (progress ring) — INLINE FIX (tooltip portion)

---

## Minor findings

### F18. `<PageTitle>` is "Abdulla" on every page
- **Heuristic:** convention + Nielsen #4
- **Symptom:** Every page sets `<PageTitle>Abdulla</PageTitle>` in `MainLayout.razor`, so the browser tab never says which page you're on. Browser history is also unreadable.
- **Proposed fix:** Each page sets its own `<PageTitle>Order Batches — Abdulla</PageTitle>` (etc).
- **Effort:** S — INLINE FIX

### F19. Brand name "Abdulla" is hardcoded
- **Heuristic:** project / configuration note
- **Symptom:** Visible in NavMenu and PageTitle. If this is a customer's name, fine; if it was a placeholder, easy to rename.
- **Proposed fix:** Note for user — confirm or replace. Make it a single `--brand-name` token if you anticipate multi-tenant.
- **Effort:** S — DEFERRED (need user confirmation)

### F20. Loading uses literal "Loading…" text everywhere
- **Heuristic:** Nielsen #1 (system status) + #8 (aesthetic)
- **Symptom:** Every list shows "Loading batches…" / "Loading requests…" / "Loading…". The visual pass adds a spinner to `.loading-state`, but the next polish step is skeleton rows (shimmer placeholders) that show table structure.
- **Proposed fix:** Replace text-only states with a small set of skeleton rows that match each table's column layout. Reuse `.loading-state` for sub-second loads.
- **Effort:** M — DEFERRED

### F21. Date format inconsistency
- **Heuristic:** Nielsen #4 (consistency)
- **Symptom:** Table rows use `dd.MM.yy HH:mm`. The package-detail modal uses `dd.MM.yyyy HH:mm:ss`. The phase-log on `/test-run` uses `HH:mm:ss` only.
- **Proposed fix:** Standardise on `dd.MM.yyyy HH:mm` for primary timestamps, `HH:mm:ss` only when sub-minute precision matters. Centralise the format string.
- **Effort:** S — INLINE FIX

### F22. Toast has no manual close button + 4s auto-dismiss
- **Heuristic:** Nielsen #3 (user control)
- **Symptom:** Toasts dismiss themselves after 4s with no way to keep them on screen or close them earlier. Error toasts often disappear before the user finishes reading them.
- **Proposed fix:** Add a tiny `×` button to toasts (re-use `.modal-close-btn` style). For error toasts, extend the timer to 8s and pause on hover.
- **Effort:** S — INLINE FIX

### F23. CustomTable pagination wording differs from the new tables
- **Heuristic:** Nielsen #4 (consistency)
- **Symptom:** New tables show `1 / 3 (total 9 orders)`. CustomTable shows `Page 1 of 3 (9 items)`. Same information, two phrasings.
- **Proposed fix:** Unify to one helper component / partial. Lowest-effort: align both to `Page X of Y · N rows`.
- **Effort:** S — INLINE FIX

### F24. Confirm-box does not trap focus
- **Heuristic:** WCAG 2.4.3
- **Symptom:** The cancel-batch confirm dialog is reachable but Tab moves into the underlying batches table.
- **Proposed fix:** Same focus-trap component as F13 covers this.
- **Effort:** (subsumed by F13)

### F25. `text-mismatch` styling is a red pill — same anti-pattern as F4
- **Heuristic:** Nielsen #4 + #8
- **Symptom:** When a completed batch has `RequestedCodes != DownloadedCodes`, the Downloaded cell renders as a small red pill. Visually loud; pulls eye even when the discrepancy is benign (e.g. partial download in progress).
- **Proposed fix:** Reduce visual weight — keep the red text but drop the pill background; add a `(diff -2)` tooltip.
- **Effort:** S — INLINE FIX

### F26. Action-column `⬇` glyph renders as substitute "I" in IBM Plex Sans
- **Heuristic:** WCAG (perceivable) + visual
- **Symptom:** Documented during the visual pass; IBM Plex Sans does not include U+2B07.
- **Proposed fix:** Replace `⬇` with `↓` (U+2193) in `Batches.razor` and `Containers.razor`. Add `aria-label="Download"` while we're at it (per F6).
- **Effort:** S — INLINE FIX (combined with F6)

---

## Inline-fix candidates (clear wins, ship as one batch)

These are CSS or single-file Razor edits that don't change info architecture and that any reasonable code review will pass. Implement these inline before deferring anything to user approval:

1. **F1** — Esc closes modals (3 modals + confirm box)
2. **F2** — bind labels to inputs via `for=`/`id=`
3. **F3** — logo `href="/"` + `aria-label`
4. **F4** — replace `.text-danger`-styled asterisk with `.required-mark`
5. **F5** — not-found page gets a proper layout + home link
6. **F6 + F26** — `aria-label` on icon buttons + swap `⬇`→`↓` and `↺`→`↻`
7. **F7** — `/products` and `/emission` adopt `.page-header` chrome
8. **F8** — CustomTable always renders the `<table>` even when empty
9. **F12** — `role="dialog"` + `aria-modal` + `aria-labelledby` on every modal
10. **F14** (low-effort branch) — remove `cursor: pointer` from non-sortable `<th>`
11. **F15** — `CustomTable` filter gets a hidden label + unique id
12. **F17** (tooltip portion) — `title="Auto-refreshing every Ns"` on the refresh indicator
13. **F18** — per-page `<PageTitle>`
14. **F21** — single date helper for `dd.MM.yyyy HH:mm`
15. **F22** — manual-close on toasts + longer timeout for errors
16. **F23** — unify pagination wording
17. **F25** — soften `text-mismatch` pill

## Deferred (user review)

- **F9** — Decide fate of `/emission` page (keep & port, or delete)
- **F10 + F11** — Persist UI state in URL (selected row, split-pane size, pagination, filter, sort) — design pattern + touches services
- **F13 + F24** — Focus-trap shared component
- **F14 (full)** — Implement column sort on Batches / Containers
- **F16** — Reprocess confirm policy (which actions need a dialog, which don't)
- **F19** — Confirm "Abdulla" brand string
- **F20** — Skeleton loaders for tables

## Out of scope (noted for future)

- Multi-tenant theming via the existing CSS-variable system
- A dedicated `<Modal>`, `<DataTable>`, and `<StatusBadge>` shared component layer to retire `CustomTable` and the duplicated modal markup
- Real-time updates via SignalR (the hub infrastructure exists but is commented out in `Program.cs`)
- Internationalisation — all UI strings are currently English-only despite the Tajikistan integration context
