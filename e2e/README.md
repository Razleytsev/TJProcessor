# TJProcessor E2E Tests

Playwright end-to-end tests for the Blazor Server frontend (`TJConnector.Web`).

## Prerequisites

- Node 20+
- Both API (`http://localhost:5166`) and Web (`http://localhost:5113`) must be running.

## Install

```bash
cd e2e
npm install
npx playwright install --with-deps chromium
```

## Run

```bash
npm test                  # headless
npm run test:headed       # watch in browser
npm run report            # open last HTML report
```

`BASE_URL` env var overrides the default `http://localhost:5113`.

## Layout

- `tests/` — one `.spec.ts` per page plus cross-cutting concerns
  - `batches.spec.ts` — happy path: load, modal open/close, row selection
  - `containers.spec.ts` — load and submit-modal interactions
  - `products.spec.ts` — load and filter
  - `test-run.spec.ts` — gate render and form validation
  - `navigation.spec.ts` — sidebar links, logo→home, browser back, 404
  - `accessibility.spec.ts` — focus visible, modal Esc, aria labels
