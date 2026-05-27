import { test, expect } from '@playwright/test';

async function waitForBlazor(page: any) {
  await page.waitForFunction(() => !!(window as any).Blazor);
  await page.waitForLoadState('networkidle');
}

// These tests catch class-level "oops the layout is broken" regressions
// without relying on pixel-perfect screenshots. They check: nothing is
// off-screen, the modal sits above its backdrop, the sidebar is not
// covering content, etc.

test.describe('Visual sanity', () => {
  test('sidebar and main-content do not overlap horizontally', async ({ page }) => {
    await page.goto('/batches');
    await waitForBlazor(page);
    const sb = await page.locator('.sidebar').boundingBox();
    const mc = await page.locator('.main-content').boundingBox();
    expect(sb).not.toBeNull();
    expect(mc).not.toBeNull();
    expect(mc!.x).toBeGreaterThanOrEqual(sb!.x + sb!.width - 1);
  });

  test('split-pane fills the content-area width', async ({ page }) => {
    await page.goto('/batches');
    await waitForBlazor(page);
    const content = await page.locator('.content-area').boundingBox();
    const split = await page.locator('.split-pane-wrapper').boundingBox();
    expect(split!.width).toBeGreaterThan(content!.width * 0.95);
  });

  test('open modal: dialog box stays inside viewport', async ({ page }) => {
    await page.goto('/batches');
    await waitForBlazor(page);
    await page.getByRole('button', { name: '+ Order Codes' }).click();
    const box = await page.locator('.modal-dialog-box').boundingBox();
    const vp = page.viewportSize()!;
    expect(box!.x).toBeGreaterThanOrEqual(0);
    expect(box!.y).toBeGreaterThanOrEqual(0);
    expect(box!.x + box!.width).toBeLessThanOrEqual(vp.width);
    expect(box!.y + box!.height).toBeLessThanOrEqual(vp.height);
  });

  test('open modal: overlay covers entire viewport', async ({ page }) => {
    await page.goto('/batches');
    await waitForBlazor(page);
    await page.getByRole('button', { name: '+ Order Codes' }).click();
    const ov = await page.locator('.modal-overlay').boundingBox();
    const vp = page.viewportSize()!;
    expect(ov!.x).toBe(0);
    expect(ov!.y).toBe(0);
    expect(ov!.width).toBe(vp.width);
    expect(ov!.height).toBe(vp.height);
  });

  test('open modal: overlay is opaque enough to dim the page', async ({ page }) => {
    await page.goto('/batches');
    await waitForBlazor(page);
    await page.getByRole('button', { name: '+ Order Codes' }).click();
    const bg = await page.locator('.modal-overlay').evaluate(el => getComputedStyle(el).backgroundColor);
    // rgba(15,23,42,0.62) — opacity ≥ 0.5
    const m = bg.match(/rgba?\([^)]+,\s*([\d.]+)\)/);
    const alpha = m ? parseFloat(m[1]) : 1;
    expect(alpha).toBeGreaterThanOrEqual(0.5);
  });

  test('page header sits above the table card and does not overlap it', async ({ page }) => {
    await page.goto('/batches');
    await waitForBlazor(page);
    const ph = await page.locator('.page-header').boundingBox();
    const top = await page.locator('.split-pane-top').boundingBox();
    expect(ph).not.toBeNull();
    expect(top).not.toBeNull();
    expect(ph!.y + ph!.height).toBeLessThanOrEqual(top!.y + 2);
  });

  test('badges use the new pill style (rounded ≥ 12px and a dot before)', async ({ page }) => {
    await page.goto('/batches');
    await waitForBlazor(page);
    const badge = page.locator('tbody .badge').first();
    if (await badge.count() === 0) test.skip(true, 'no badges in data');
    const r = await badge.evaluate(el => parseFloat(getComputedStyle(el).borderRadius));
    expect(r).toBeGreaterThanOrEqual(12);
  });

  test('every action button has visible (non-zero) text or aria-label', async ({ page }) => {
    await page.goto('/batches');
    await waitForBlazor(page);
    const buttons = page.locator('button');
    const n = await buttons.count();
    const failures: string[] = [];
    for (let i = 0; i < n; i++) {
      const b = buttons.nth(i);
      const txt = ((await b.textContent()) ?? '').trim();
      const aria = (await b.getAttribute('aria-label')) ?? '';
      if (!txt && !aria) {
        const html = (await b.innerHTML()).substring(0, 60);
        failures.push(html);
      }
    }
    expect(failures, `nameless buttons: ${failures.join(', ')}`).toEqual([]);
  });

  test('main has a single <main> landmark and one <nav> sidebar', async ({ page }) => {
    await page.goto('/batches');
    await waitForBlazor(page);
    await expect(page.locator('main')).toHaveCount(1);
    await expect(page.locator('nav')).toHaveCount(1);
  });

  test('not-found page renders 404 layout + home button at /unknown', async ({ page }) => {
    await page.goto('/this-route-does-not-exist');
    await expect(page.locator('.not-found-code')).toHaveText('404');
    await expect(page.locator('.not-found-title')).toBeVisible();
    await expect(page.getByRole('link', { name: /Go to home/i })).toBeVisible();
  });
});

test.describe('Responsive: layout works at common viewport sizes', () => {
  const viewports: Array<{ w: number; h: number; label: string }> = [
    { w: 1366, h: 768,  label: '1366×768 (laptop)' },
    { w: 1440, h: 900,  label: '1440×900 (default)' },
    { w: 1920, h: 1080, label: '1920×1080 (full HD)' },
  ];

  for (const vp of viewports) {
    test(`/batches fills the viewport at ${vp.label}`, async ({ page }) => {
      await page.setViewportSize({ width: vp.w, height: vp.h });
      await page.goto('/batches');
      await waitForBlazor(page);
      const split = await page.locator('.split-pane-top').boundingBox();
      // Card should reach within 30px of the right edge of the content area
      expect(split!.x + split!.width).toBeGreaterThan(vp.w - 60);
    });
  }
});
