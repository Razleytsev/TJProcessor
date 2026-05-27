# Visual Bugs — 2026-05-27

Method: Playwright MCP walkthrough at 1366×768 / 1440×900 / 1920×1080, every
page (/batches, /containers, /products, /test-run, /emission, 404), every
modal (Order Codes, Submit Containers, Package detail, Cancel-Confirm),
every action (row click, hover, validation, autocomplete, download).
Each finding cross-checked against the .razor source and `site.css`.

Stack version at audit start: `4e4ab2f` (master).
Screenshots: `docs/frontend-audit/2026-05-27-visual-bugs-screenshots/`.

## Summary
| Severity | Count |
|---|---|
| Critical | 2 |
| Major    | 6 |
| Minor    | 6 |
| **Total**| **14** |

The Changer aesthetic still reads well and the previous round of fixes
(F1 placeholder, F3 outline buttons, F6 select caret, F12 muted text, F13
preview empty-state, F15 Recent-runs card) are confirmed shipped. The two
critical bugs found below were introduced by carrying over a Bootstrap
`sticky-top` thead pattern that breaks the new modal overlay, and by a
silent-failure on `/products` row click that leaves the user with no
feedback when the external API is unreachable.

---

## Critical

### B1. Sticky table headers (`thead.sticky-top`) bleed through every modal
- **Where:** every page that uses `.pane-scroll thead.sticky-top` —
  observed on `/batches` (Order Codes, Cancel-Confirm), `/containers`
  (Submit Containers, Package detail).
- **CSS file:** `TJConnector.Web/wwwroot/css/site.css` line 654 + the
  Bootstrap default `.sticky-top { z-index: 1020 }` from
  `wwwroot/css/bootstrap/bootstrap.min.css`.
- **Viewport(s):** 1366 / 1440 / 1920 (all).
- **What's wrong:** `.modal-overlay` is set to `z-index: 1000`
  (site.css:1290) but Bootstrap's `.sticky-top` utility class promotes
  the page table `<thead>` to `z-index: 1020`. Result: page-behind column
  headers render ON TOP of the modal body. Confirmed via
  `document.elementsFromPoint(700, 537)` returning `<th>Requested</th>`
  while the modal-overlay is mounted.
- **Why it matters:** in the Order-Codes modal the "MARKING LINE" label
  is completely hidden behind the leaked-through page row "ID  EXTERNAL
  GUID  REQUESTED  DOWNLOADED  …" (screenshot 03b-order-codes-zoom.png).
  Operators see a select with no label and a strip of unrelated table
  text floating across the dialog. Same effect inside the Cancel-Confirm
  dialog and the Package-Detail modal (screenshot 07-package-detail).
- **Fix hint:** raise `.modal-overlay` to `z-index: 1100` (or any value
  ≥ 1021), OR override Bootstrap's class:
  ```css
  .modal-overlay { z-index: 1100; }
  /* and ensure the pane-scroll override actually lands on the thead: */
  .pane-scroll thead.sticky-top,
  .pane-scroll thead.sticky-top th { z-index: 2; }
  ```
  Note: the existing `.pane-scroll thead.sticky-top th` rule on
  site.css:654 targets the `<th>` only — the `<thead>` itself still
  inherits Bootstrap's 1020.
- **Severity rationale:** content of every modal is partially obscured;
  worst case (Marking Line label) the user cannot read a form label.

### B2. Clicking a product row on `/products` gives zero feedback when external API fails
- **Where:** `TJConnector.Web/Pages/Products.razor` lines 24-40 and
  `OnProductClick` 90-111.
- **Viewport(s):** 1366 / 1440 / 1920 (all).
- **What's wrong:** the sliding details panel is conditional on
  `selectedProductExternalInfo != null`. The `OnProductClick` handler
  swallows external-API errors with `Console.WriteLine` and sets the
  field back to `null`. When the external Marking-Authority API is
  unreachable (current docker setup), clicking any row only changes the
  row highlight — no panel, no error toast, no inline message. Verified:
  `document.querySelectorAll('[class*="detail"]').length === 0` after
  clicking the Mastercase row.
- **Why it matters:** the page's main affordance is "click for details"
  and it visibly does nothing. Indistinguishable from a frozen UI.
- **Fix hint:** in `Products.razor` always render the
  `.details-container` once a row is selected; show a "Loading external
  info…" state, then either render the data or an `.alert.alert-warning`
  with the external API error message. Don't gate the whole panel on
  `selectedProductExternalInfo != null`; gate only its body content.
- **Severity rationale:** primary interaction silently does nothing.

---

## Major

### B3. Disabled `<select>` controls lose their dropdown caret entirely
- **Where:** Order-Codes modal → Factory + Marking Line selects on
  `/batches` (Pages/Batches.razor). Disabled-select pattern likely the
  same on `/test-run`.
- **Viewport(s):** all.
- **What's wrong:** F6 (previous audit) restored a background-image
  caret on `.form-select`, but the disabled state has
  `background-image: none`. Computed style on the disabled Factory
  select: `background-image: "none"`, while the enabled Type select
  carries the SVG caret. Result: disabled selects look identical to a
  read-only text input.
- **Fix hint:** keep the caret SVG on `.form-select:disabled` but draw
  it at 40% opacity, or use `filter: grayscale(1) opacity(0.5)` so the
  affordance is preserved but visually de-emphasised.
- **Severity rationale:** Norman affordance — users can't tell it's a
  control at all.

### B4. Status column on `/containers` top table renders plain text "Created" — no badge
- **Where:** `/containers` Requests table, Status column. Compare to
  the Packages sub-table (same page) which uses proper badges.
- **CSS file / template:** `TJConnector.Web/Pages/Containers.razor` —
  the Status cell renders only `@request.Status.ToString()` instead of
  the `.badge .badge-{state}` markup.
- **Viewport(s):** all.
- **What's wrong:** every status text is rendered as bare body text
  ("Created" / "Created" / "Created"), inconsistent with `/batches`
  (badge+dot), Package detail (badge+dot), and the same page's lower
  pane (`Reported`, `App: Processing`, `Agg: Send error` badges).
  Screenshot 05-containers-1440.png.
- **Fix hint:** in Containers.razor, wrap the status text in
  `<span class="badge badge-@status.ToString().ToLower()">`
  and add `badge-created` to site.css if missing.
- **Severity rationale:** visual inconsistency within a single page, and
  loses the at-a-glance state signal the rest of the app relies on.

### B5. Date format is inconsistent across the app
- **Where:** every table with a Date column + the Package-detail modal.
- **Viewport(s):** all.
- **What's wrong:** 
  - `/batches` Batches & Orders tables → `27.05.26 11:52` (2-digit year, no seconds)
  - `/containers` Requests table → `27.05.26 12:17` (same as batches)
  - `/containers` Packages table → `26.05.26 16:25` (same)
  - Package-detail modal RECORD DATE field → `26.05.2026 16:25:59` (4-digit year, with seconds)
  - Package-detail modal Status History rows → `26.05.26 17:25:59` (2-digit year, with seconds)
  - `/emission` External Orders table → `27.05.2026 11:52:04` (4-digit year, with seconds)
  - `/products` Record Date → `12.04.2026 12:22:04` (4-digit year, with seconds)
  Three different formats coexist on the same screen.
- **Fix hint:** centralise via a single helper (already exists as
  `LTime` on Products.razor:113 — `"dd.MM.yyyy HH:mm:ss"`). Pick ONE
  format for tables (`dd.MM.yy HH:mm` is the most compact, current
  /batches style) and apply it consistently. The Package-detail modal
  appears to use two different formatters in adjacent rows.
- **Severity rationale:** reading dates mentally requires reparsing
  between rows; suggests subtle bug in formatting layer.

### B6. Package-detail modal exposes raw integer status code `(12)` next to the badge
- **Where:** `/containers` → click any package row → Package detail
  modal, STATUS row.
- **Viewport(s):** all.
- **What's wrong:** the STATUS row renders `[Reported] (12)` — the
  human label correctly + a raw integer "(12)" in parentheses. Source
  is `Pages/Containers.razor` (the package-detail dt/dd block), which
  appends `(@package.Status)` to the badge. Screenshot
  07-package-detail-1440.png.
- **Why it matters:** the integer is internal-only and exposes
  implementation. It reads like a count to operators.
- **Fix hint:** drop the `(@package.Status)` integer rendering — the
  badge text alone is sufficient. If a status-code is useful for
  support, move it to a tooltip on the badge or a tiny mono text in a
  diagnostic block.
- **Severity rationale:** confusing user-facing content.

### B7. Heading semantics + auto-refresh placement are inconsistent across pages
- **Where:** Pages/Batches.razor, Containers.razor, Products.razor,
  TestRun.razor.
- **Viewport(s):** all.
- **What's wrong:** 
  - `/products` uses `<h1 class="mb-0">Products</h1>`
  - `/batches` uses `<h5>Order Batches</h5>`
  - `/containers` uses `<h5>Submission Requests</h5>`
  - `/test-run` uses `<h2>` (need to verify but the visual fits)
  All four render at the same visual size (21.6px / 600 weight) but the
  semantic levels differ — F1 of a screen-reader audit. Additionally,
  the auto-refresh pill (`5 s`, `30 s`) sits *inside* the title row on
  `/batches` + `/containers` but `/products` and `/test-run` don't have
  one at all, and `/test-run` puts its "(auto-refresh 2 s)" hint
  *inside* the Recent-runs card title.
- **Fix hint:** pick one — `<h1 class="page-title">` on every page +
  consistent `.page-header` markup so the auto-refresh pill, title, and
  primary CTA share a single header strip pattern.
- **Severity rationale:** accessibility + visual rhythm regression
  across navigation.

### B8. Disabled `btn-outline-primary` buttons keep `cursor: pointer` and primary blue colour
- **Where:** `/test-run` External-DB-Test card → "Test Content Query"
  + "Test Info Query" buttons.
- **Viewport(s):** all.
- **What's wrong:** computed style on the disabled buttons:
  `cursor: pointer; opacity: 0.65; color: rgb(13, 110, 253);
  border-color: rgb(37, 99, 235)`. The Bootstrap default for disabled
  buttons doesn't switch `cursor` and Blazor's `disabled` attribute
  just dims via opacity. They look enabled-but-faded and they invite a
  click that never fires.
- **Fix hint:** add to site.css:
  ```css
  .btn:disabled, .btn[disabled] { cursor: not-allowed; pointer-events: auto; }
  .btn-outline-primary:disabled { color: var(--text-muted); border-color: var(--border-soft); }
  ```
- **Severity rationale:** Norman feedback — the affordance lies about
  state.

---

## Minor

### B9. Status History dates listed newest-first while RECORD DATE is the oldest event
- **Where:** Package-detail modal → Status History table.
- **Viewport(s):** all.
- **What's wrong:** RECORD DATE shows `16:25:59`; the history table
  lists `Reported 17:25:59` then `Created 16:25:59`. Visually it looks
  like the "Record Date" doesn't match the last entry. Cognitive load.
- **Fix hint:** either flip the sort to oldest-first, OR label the
  RECORD DATE explicitly "Created at" so the relationship is clear.

### B10. Selected row highlight persists across navigation without showing its detail panel
- **Where:** `/containers` — after a refresh, row #8 stayed highlighted
  but the bottom pane still said "Click a request row above to view its
  packages." Same on `/batches` after navigation. Server-side Blazor
  state is preserved but the UI doesn't fully restore.
- **Fix hint:** on `OnInitializedAsync`, either restore both selection
  + detail data, or clear selection so the row stops looking selected.

### B11. Reprocess single-package button is icon-only with no tooltip, inconsistent with labeled "Reprocess All Errors"
- **Where:** `/containers` Packages sub-table, last column on rows with
  `Agg: Send error` / `App: Send error` etc.
- **What's wrong:** the per-row button is just `↻` (24×24-ish), no
  visible label, no tooltip. The pane-toolbar above uses
  `↻ Reprocess All Errors (1)` with text. Pattern inconsistency + no
  `title` attribute means hover gives no hint either.
- **Fix hint:** add a `title` attribute ("Reprocess this package") and
  consider a label-on-hover or always-visible "Retry" text for parity.

### B12. ASCII glyphs (`↓`, `↻`, `▶`, `+`, `×`, `📂`) leak through everywhere as substitutes for proper icons
- **Where:** Order Codes button (`+`), Download button (`↓`), Reprocess
  buttons (`↻`), Start Test Run (`▶`), modal close (`×`), Submit-
  Containers "📂 Load from file" (emoji folder).
- **What's wrong:** each glyph relies on the system font's rendering;
  the folder emoji renders coloured/3D on most OSes which clashes with
  the monochrome iconography elsewhere. Sizes are unequal — `×` looks
  thin next to `↓` looks chunky.
- **Fix hint:** introduce a small inline-SVG icon-set (Heroicons or
  Lucide) and swap glyphs one-by-one. Same recommendation as previous
  F17.

### B13. `(auto-refresh 2 s)` hint on Test Run is appended in the card title in muted text — easy to mistake for a subtitle
- **Where:** `/test-run` → Recent runs card.
- **What's wrong:** the text is rendered inline with the "Recent runs"
  heading using `.text-xs.text-muted`, but at certain font-renderings
  the parens visually disconnect from "Recent runs". On batches /
  containers the same info is a pill on the page-header. Inconsistent
  pattern.
- **Fix hint:** move the refresh indicator to the right edge of the
  card header (matching `/batches` style) so the pattern is one thing.

### B14. Order-Codes "Cancel" + Cancel-Confirm "No" + Package-detail "Close" footer-buttons use three different cancel labels
- **Where:** modals across the app.
- **What's wrong:** Order-Codes uses "Cancel" + "Create Order"; Cancel-
  Confirm uses "Yes, Cancel" + "No"; Package-detail uses just "Close".
  Each is reasonable in isolation but the operator switches mental
  models per modal.
- **Fix hint:** standardise — *destructive confirmations* use
  "Yes, <action>" + "Cancel"; *form modals* use "Cancel" +
  "<verb>"; *info / read-only* modals use "Close". Currently the
  Cancel-Confirm dialog uses "No" instead of "Cancel".

---

## Cross-cutting observations

- The previous audit's F2 (Products: loading + empty-state shown at
  the same time) is **fully fixed** — the page now renders only the
  loading-state until products are non-null.
- The previous audit's F1 (literal `&#10;` in Submit Containers
  textarea placeholder) is **fully fixed** — textarea placeholder
  contains real newline characters.
- The previous audit's F12 (muted text reading as accent-blue) is
  **fixed** — the `.text-muted` color is now `rgb(100, 116, 139)`
  (slate-500) as intended.
- The Changer modal-overlay (rgba 0.62 + 4px blur) reads correctly;
  the only stacking issue is B1.
- The seeded data has "Defaul" instead of "Default" on factory,
  marking line, location entities. Not a CSS bug but visible to users
  — flag to the seed-data owner separately.
- `/emission` is reachable by URL but absent from sidebar nav —
  intentional? If yes, it should be redirected to /batches or 404 to
  avoid orphan-page confusion.
- No live regions / aria announcements for status-change toasts; the
  toast is rendered but a screen-reader user won't hear it. Out of
  scope for this visual audit, noted for accessibility pass.
