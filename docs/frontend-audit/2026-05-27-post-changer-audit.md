# Post-Changer-Adoption UX Audit — 2026-05-27

Screenshots: `docs/frontend-audit/2026-05-27-post-changer-audit/`
Reference: the visual system adopted in commit `5c13d7c` (Changer
patterns from `H:\Legacy-do-not-adopt-only-check\Changer`).
Method: real-flow walkthrough at 1440 × 900 in Playwright MCP; mouse
+ keyboard interaction; cross-referenced against the .razor sources.

## Summary

| Severity | Count |
|---|---|
| Critical | 2  |
| Major    | 9  |
| Minor    | 8  |
| **Total**| **19** |

The Changer aesthetic is fully on. Most remaining issues are loose
ends from the adoption (a leftover Bootstrap-style select that no
longer matches, two overlapping empty-state messages on Products) or
small UI inconsistencies that the visual refresh exposed.

---

## Critical findings

### F1. Submit-Containers textarea placeholder shows literal `&#10;`
- **Page / flow:** `/containers` → click `+ Submit Containers`
- **Heuristic:** Nielsen #2 (match between system and real world) + correctness
- **Symptom:** The textarea's placeholder renders literally as
  `Code line&#10;SSCC line&#10;Code line&#10;SSCC line…` — Blazor
  double-encodes the `&` so the HTML entity never gets decoded.
- **Why this matters:** the operator's first visual cue for the
  required format is unintelligible — a competent user will think
  the app is broken before they paste anything.
- **Proposed fix:** in `Pages/Containers.razor`, replace
  `placeholder="Code line&#10;SSCC line&#10;…"` with a multi-line
  C# string that contains real newline characters, e.g.
  `placeholder="@("Code line\nSSCC line\nCode line\nSSCC line…")"`.
- **Effort:** S — INLINE FIX

### F2. Products page shows "No items found" AND "Loading products…" at the same time
- **Page / flow:** `/products` while the external-products call is in flight
- **Heuristic:** Nielsen #1 (visibility of system status) + #4 (consistency)
- **Symptom:** the table body says "No items found" (CustomTable's
  empty-state) while a "Loading products…" spinner sits under the
  table — contradictory signals. The user can't tell whether the
  list is genuinely empty or still arriving.
- **Why this matters:** the page can sit in this contradictory state
  for several seconds while the external GTIN fetch runs.
- **Proposed fix:** in `Pages/Products.razor`, render the
  `loading-state` instead of the CustomTable when `products == null`.
  Once it's a real (possibly empty) collection, hand it to the
  CustomTable.
- **Effort:** S — INLINE FIX

---

## Major findings

### F3. Cancel button uses dark-grey `btn-secondary` — clashes with the Changer system
- **Pages:** Order-Codes modal, Submit-Containers modal, confirm box
- **Heuristic:** Nielsen #4 (consistency)
- **Symptom:** the Changer system uses `btn-outline` for secondary
  actions (transparent + grey border). Our modals use Bootstrap
  `btn-secondary` (filled dark grey), so the dialog footer looks
  loud and visually equal to the primary action.
- **Proposed fix:** replace `class="btn btn-sm btn-secondary"` with
  `class="btn btn-sm btn-outline"` (or `btn-outline-secondary`) in
  all three places.
- **Effort:** S — INLINE FIX

### F4. Empty-state rendered AND wrapped in panel — double border
- **Pages:** `/batches`, `/containers` empty states; "Click a row in
  the top pane" pane
- **Heuristic:** Nielsen #8 (aesthetic / information density)
- **Symptom:** `.empty-state` has a 1px dashed border, sitting inside
  `.split-pane-top` which has a 1px solid border. Two borders + a
  card shadow + the dashed inner border = visual noise for a single
  "nothing here yet" message.
- **Proposed fix:** when the empty state lives INSIDE a card-style
  pane, drop its dashed border. Add a `.empty-state.flush` variant
  (or just make the bare `.empty-state` borderless and adopt
  `.empty-state.bordered` for the standalone case).
- **Effort:** S — INLINE FIX

### F5. Auto-refresh indicator "↻ 30 s" placed mid-page-header looks like part of the title
- **Pages:** `/batches`, `/containers`
- **Heuristic:** Nielsen #4 + Norman (visibility, mapping)
- **Symptom:** the `↻ 30 s` text is rendered between the page title
  and the primary CTA at the same baseline. It reads as part of the
  header sentence.
- **Proposed fix:** move it to the right of the action button, or
  to the top-bar (where Changer puts utility status). Style as a
  tiny pill that pulses while `isRefreshing == true`.
- **Effort:** S (move) or M (proper pill) — INLINE FIX

### F6. `<select>` controls have no visible caret (no dropdown affordance)
- **Pages:** every form (Order Codes modal, Submit Containers, Test Run)
- **Heuristic:** Norman (affordance) + WCAG (perceivable)
- **Symptom:** the form-control / form-select styling overrides
  Bootstrap's appearance and we never restored a caret/arrow glyph,
  so a `<select>` looks identical to a text input.
- **Proposed fix:** add a CSS background-image arrow (SVG data URI)
  on `.form-select`, or set `appearance: auto` so the native arrow
  renders.
- **Effort:** S — INLINE FIX

### F7. Disabled `<select>` (auto-selected single option) looks editable
- **Pages:** Order-Codes modal → Factory, Marking Line
- **Heuristic:** Nielsen #6 (recognition) + Norman (constraint)
- **Symptom:** when there's only one factory/line, the dropdown is
  set to `disabled` but visually it looks like an active select.
  Operators try to click it.
- **Proposed fix:** in addition to F6's caret, give disabled selects
  a clearly-readonly look: lighter background, locked-cursor on
  hover, no border-hover effect.
- **Effort:** S — INLINE FIX

### F8. `BATCHES (0)` / `REQUESTS (0)` toolbar above an empty-state is redundant
- **Pages:** `/batches`, `/containers`
- **Heuristic:** Nielsen #8 (minimalist design)
- **Symptom:** the pane toolbar shows "BATCHES (0)" right above
  "No batches. Click + Order Codes to create one." Same number, same
  state, two lines of UI.
- **Proposed fix:** hide the count toolbar when the pane has zero
  rows, OR drop the empty-state and rely on the toolbar count + a
  pinned hint inside the empty area. Lowest-effort: hide the
  `.pane-toolbar` row when count == 0.
- **Effort:** S — INLINE FIX

### F9. Validation reports one field at a time
- **Page / flow:** Order-Codes modal → Create without filling
- **Heuristic:** Nielsen #5 (error prevention) + #9 (recover)
- **Symptom:** the form validation early-returns at the first failed
  rule. User sees "Codes count is required" but no hint that GTIN
  is also missing. Fixing one field reveals the next error.
- **Proposed fix:** collect all validation errors before returning;
  mark each invalid field with its inline message.
- **Effort:** M — DEFERRED

### F10. `<main>` content area scrolls inside, but the page itself has a static height
- **Pages:** any split-pane page
- **Heuristic:** convention; Nielsen #3
- **Symptom:** the new `.content-area` uses `flex: 1; min-height: 0`
  so split-pane pages don't trigger a window scrollbar. Looks fine
  on a tall window; on a short one the pane content scrolls but the
  split-pane divider can be cut off.
- **Proposed fix:** ensure `.main-content` has `min-height: 100vh`
  or set an explicit min-height on `.content-area` so the divider
  is always reachable. Test at 700px height.
- **Effort:** S — INLINE FIX

### F11. Top-bar shows static "PRODUCTION-TEST CONTOUR" label but no live status / context
- **Pages:** every page
- **Heuristic:** Nielsen #1 (system status) + #10 (help)
- **Symptom:** the top-bar is mostly empty; one decorative label.
  It could surface useful info: current environment name (Dev /
  Prod), API connectivity dot, last DB sync timestamp.
- **Proposed fix:** add a small connectivity indicator (`api ok`
  green dot vs red) using a heartbeat ping to `/api/health` (or any
  cheap endpoint). Keep the env label, add the dot to its left.
- **Effort:** M — DEFERRED

---

## Minor findings

### F12. `.text-xs.text-muted` description on Test Run renders in accent-blue, not muted-grey
- **Page:** `/test-run`
- **Heuristic:** Nielsen #4
- **Symptom:** the descriptive paragraph "Drives the full marking-
  authority chain (pack → bundle → mastercase → application →
  aggregation) against the real external system." renders in
  accent-blue because the parent has a stray colour cascade. Should
  be `--text-muted`.
- **Proposed fix:** investigate the inline-style/inherited colour;
  add `color: var(--text-muted) !important` on
  `.text-muted` if not already present (it is — so the cascade
  override must come from a sibling rule, likely `.alert` or the
  parent card).
- **Effort:** S — INLINE FIX

### F13. Submit-Containers preview table has no empty-state when there are 0 pairs
- **Page / flow:** Submit Containers modal at initial open
- **Heuristic:** Nielsen #1
- **Symptom:** the right-hand preview panel shows "PREVIEW (0 PAIRS)"
  header then a bare column row, then nothing. No hint of "paste
  pairs on the left to see them here".
- **Proposed fix:** render an inline empty-state row "Paste pairs on
  the left to preview." inside `<tbody>` when `packageCouples.Count
  == 0`.
- **Effort:** S — INLINE FIX

### F14. Sidebar version-tag is hardcoded `v1.0`
- **Page:** every page
- **Heuristic:** Nielsen #1 (system status)
- **Symptom:** `<span class="version-tag">v1.0</span>` is a literal.
  It always says v1.0 regardless of the actual build.
- **Proposed fix:** read the assembly's `InformationalVersion` or a
  Git short-sha at build time and inject it into the layout. For
  now, at least pull from a single constant.
- **Effort:** M — DEFERRED

### F15. Test-Run "Recent runs" section has no card / border
- **Page:** `/test-run`
- **Heuristic:** Nielsen #4 (consistency)
- **Symptom:** the form section is inside a `.card` style, the
  External-DB-Test section is in a card, but "Recent runs" sits
  bare against the canvas. Inconsistent grouping.
- **Proposed fix:** wrap "Recent runs" in the same card pattern
  (`<div class="info-card">...</div>`).
- **Effort:** S — INLINE FIX

### F16. Modal close `×` button is a thin character, not a glyph
- **Pages:** every modal
- **Heuristic:** WCAG / hit-target size
- **Symptom:** the `×` is a 1.4rem character with 2px×6px padding.
  Hit area is ~24×24 px. Spec asks ≥ 44×44.
- **Proposed fix:** bump padding on `.modal-close-btn` to 6px × 10px;
  swap to a 16px-line SVG `×` for crispness at 1× and 2× DPI.
- **Effort:** S — INLINE FIX

### F17. `+ Order Codes` button text reads like a label, not a CTA
- **Page:** `/batches`, `/containers` (`+ Submit Containers`)
- **Heuristic:** Norman (signifier) + convention
- **Symptom:** the leading `+` is a code-art character. Other admin
  tools use real icons (Heroicons / Lucide / Iconify).
- **Proposed fix:** introduce an inline SVG `<svg class="btn-icon">`
  pattern for `+ / ↻ / ↓` and drop the literal glyphs.
- **Effort:** M — DEFERRED

### F18. `.pane-toolbar` height jumps when an action button appears
- **Page:** `/containers` → select a request with errors
- **Heuristic:** Nielsen #4 (consistency)
- **Symptom:** the toolbar is `min-height: 36px` when empty but
  grows to fit a button when one is added. Layout shifts.
- **Proposed fix:** raise `min-height` to whatever the
  `+ Reprocess All Errors` row needs, so the toolbar reserves the
  vertical space regardless of state.
- **Effort:** S — INLINE FIX

### F19. CustomTable filter input shows placeholder "Filter…" but no icon — looks like a tiny stranded text box
- **Page:** `/products`
- **Heuristic:** Norman (signifier)
- **Symptom:** the filter input is a `<input type="search">` with no
  leading magnifier icon — at a glance it looks like a search field
  the user has to discover.
- **Proposed fix:** add a leading SVG magnifier (Heroicons
  `magnifying-glass`) via a background-image, or wrap the input in
  a `.search-input` container.
- **Effort:** S — INLINE FIX

---

## Inline-fix candidates (ship as one batch)

1. F1 — fix `&#10;` placeholder encoding
2. F2 — Products: show loading-state OR empty-state, not both
3. F3 — Cancel buttons → `btn-outline-secondary`
4. F4 — drop double-bordered `.empty-state` inside `.split-pane-top`
5. F5 — relocate auto-refresh indicator + style as pill
6. F6 — restore caret on `<select>` via SVG background
7. F7 — disabled-select visual treatment (subsumed by F6)
8. F8 — hide pane-toolbar count when row count is zero
9. F10 — set min-height on .content-area so divider stays visible
10. F12 — fix `.text-muted` cascade override on Test Run description
11. F13 — Submit-Containers empty preview hint
12. F15 — wrap Test Run "Recent runs" in info-card
13. F16 — bump modal close button hit area
14. F18 — reserve pane-toolbar height to prevent layout shift
15. F19 — add magnifier icon to CustomTable filter input

## Deferred (user review)

- **F9** — collect all validation errors at once (not first-fail)
- **F11** — live API health indicator in top-bar
- **F14** — assembly-version-driven sidebar version tag
- **F17** — replace ASCII `+ / ↻ / ↓` glyphs with proper icon set

## Out of scope (noted for future)

- Hover-card on long ExternalGuid / SSCC code cells (Changer pattern;
  TJProcessor doesn't surface them yet)
- Skeleton loaders for the batches / containers / packages tables
  (TableSkeleton component exists in CSS but is never rendered)
- Multi-select filter for batch status (Changer pattern; TJ tables
  don't have a filter row yet)
