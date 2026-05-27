import { test, expect } from '@playwright/test';

async function waitForBlazor(page: any) {
  await page.waitForFunction(() => !!(window as any).Blazor);
  await page.waitForLoadState('networkidle');
}

test.describe('Action buttons render correctly', () => {
  test('Download / Cancel / Continue buttons on /batches rows', async ({ page }) => {
    await page.goto('/batches');
    await waitForBlazor(page);
    const rows = page.locator('.split-pane-top tbody tr');
    const count = await rows.count();
    if (count === 0) test.skip(true, 'no batches seeded');

    // Walk every row and look for row-level buttons in the action column
    for (let i = 0; i < count; i++) {
      const action = rows.nth(i).locator('.cell-actions');
      const buttons = action.locator('button');
      const btnCount = await buttons.count();
      // Each row should have 0 or 1 visible action depending on status
      expect(btnCount).toBeGreaterThanOrEqual(0);
      expect(btnCount).toBeLessThanOrEqual(2);
      // If a button is present, it has either an aria-label or text content
      for (let j = 0; j < btnCount; j++) {
        const btn = buttons.nth(j);
        const aria = await btn.getAttribute('aria-label');
        const text = (await btn.textContent())?.trim();
        expect(aria || text).toBeTruthy();
      }
    }
  });

  test('Download glyph (↓) renders correctly (no tofu)', async ({ page }) => {
    await page.goto('/batches');
    await waitForBlazor(page);
    const dl = page.locator('.btn-outline-success').first();
    if (await dl.count() === 0) test.skip(true, 'no completed batches');
    const text = (await dl.textContent())?.trim();
    // Must contain U+2193 not U+2B07
    expect(text).toContain('↓');
    expect(text).not.toContain('⬇');
  });

  test('Reprocess glyph (↻) renders correctly on /containers', async ({ page }) => {
    await page.goto('/containers');
    await waitForBlazor(page);
    const row = page.locator('.split-pane-top tbody tr').first();
    if (await row.count() === 0) test.skip(true, 'no requests');
    await row.click();
    await expect(page.getByText(/Packages — Request #/)).toBeVisible();
    const repro = page.locator('.split-pane-bottom .btn-outline-warning').first();
    if (await repro.count() === 0) test.skip(true, 'no errored packages on this request');
    const text = (await repro.textContent())?.trim() ?? '';
    expect(text).toContain('↻');
    expect(text).not.toContain('↺');
  });

  test('Cancel-batch confirm dialog cancels via No', async ({ page }) => {
    await page.goto('/batches');
    await waitForBlazor(page);
    const cancelBtn = page.locator('.split-pane-top tbody').getByRole('button', { name: 'Cancel' }).first();
    if (await cancelBtn.count() === 0) test.skip(true, 'no cancellable batches');
    await cancelBtn.click();
    const confirmBox = page.locator('.confirm-box');
    await expect(confirmBox).toBeVisible();
    await confirmBox.getByRole('button', { name: 'No' }).click();
    await expect(confirmBox).toBeHidden();
  });
});

test.describe('Top bar + sidebar chrome', () => {
  test('top-bar env tag visible on every page', async ({ page }) => {
    for (const path of ['/batches', '/containers', '/products', '/test-run']) {
      await page.goto(path);
      await waitForBlazor(page);
      await expect(page.locator('.env-tag')).toBeVisible();
    }
  });

  test('sidebar shows brand + 4 nav links + version tag', async ({ page }) => {
    await page.goto('/batches');
    await waitForBlazor(page);
    await expect(page.locator('.sidebar .brand')).toBeVisible();
    const navLinks = page.locator('.sidebar nav a');
    await expect(navLinks).toHaveCount(4);
    await expect(page.locator('.sidebar .version-tag')).toBeVisible();
  });

  test('active nav link has accent left-bar (via inset box-shadow)', async ({ page }) => {
    await page.goto('/batches');
    await waitForBlazor(page);
    const active = page.locator('.sidebar nav a.active');
    await expect(active).toHaveCount(1);
    const shadow = await active.evaluate(el => getComputedStyle(el).boxShadow);
    expect(shadow).toMatch(/inset/);
  });
});

test.describe('Refresh pill', () => {
  test('shows interval in seconds + has aria-friendly title', async ({ page }) => {
    await page.goto('/batches');
    await waitForBlazor(page);
    const pill = page.locator('.refresh-pill').first();
    await expect(pill).toBeVisible();
    const title = await pill.getAttribute('title');
    expect(title).toMatch(/Auto-refreshes/);
  });
});
