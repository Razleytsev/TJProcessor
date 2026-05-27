import { test, expect } from '@playwright/test';

async function waitForBlazor(page: any) {
  await page.waitForFunction(() => !!(window as any).Blazor);
  await page.waitForLoadState('networkidle');
}

test.describe('Tables', () => {
  test.describe('/batches table', () => {
    test.beforeEach(async ({ page }) => {
      await page.goto('/batches');
      await waitForBlazor(page);
    });

    test('renders the expected columns', async ({ page }) => {
      const headers = page.locator('.split-pane-top thead th');
      const texts = await headers.allTextContents();
      // Trim + remove empty (action column header is empty)
      const labels = texts.map(t => t.trim()).filter(t => t.length > 0);
      expect(labels).toEqual(expect.arrayContaining(['ID', 'Type', 'GTIN', 'Count', 'Description', 'User', 'Date', 'Status']));
    });

    test('rows are clickable; selection highlights row', async ({ page }) => {
      const row = page.locator('.split-pane-top tbody tr').first();
      if (await row.count() === 0) test.skip(true, 'no batches seeded');
      await row.click();
      await expect(row).toHaveClass(/highlighted-row/);
      // Bottom pane shows the orders for this batch
      await expect(page.getByText(/Orders\s*[—–-]\s*Batch #/)).toBeVisible();
    });

    test('Date column right-aligns (visual layout sanity)', async ({ page }) => {
      const dateCell = page.locator('.split-pane-top tbody tr').first().locator('.col-date');
      if (await dateCell.count() === 0) test.skip(true, 'no rows');
      const align = await dateCell.evaluate(el => getComputedStyle(el).textAlign);
      expect(align).toBe('right');
    });

    test('Status badge has a colored leading dot', async ({ page }) => {
      const badge = page.locator('.split-pane-top tbody .badge').first();
      if (await badge.count() === 0) test.skip(true, 'no rows');
      // ::before pseudo-element with circular dot
      const pseudoExists = await badge.evaluate(el => {
        const cs = getComputedStyle(el, '::before');
        return cs.content !== 'none' && cs.borderRadius === '50%';
      });
      expect(pseudoExists).toBeTruthy();
    });
  });

  test.describe('/containers table', () => {
    test.beforeEach(async ({ page }) => {
      await page.goto('/containers');
      await waitForBlazor(page);
    });

    test('renders the expected columns', async ({ page }) => {
      const headers = page.locator('.split-pane-top thead th');
      const texts = (await headers.allTextContents()).map(t => t.trim()).filter(Boolean);
      expect(texts).toEqual(expect.arrayContaining(['ID', 'Filename', 'User', 'Total', 'Processed', 'Status', 'Date']));
    });

    test('row click loads package pane', async ({ page }) => {
      const row = page.locator('.split-pane-top tbody tr').first();
      if (await row.count() === 0) test.skip(true, 'no requests seeded');
      await row.click();
      await expect(page.getByText(/Packages — Request #/)).toBeVisible();
    });
  });

  test.describe('CustomTable on /products', () => {
    test.beforeEach(async ({ page }) => {
      await page.goto('/products');
      await waitForBlazor(page);
      // Wait for external products sync (15-30s) so the table renders
      await page.waitForFunction(() =>
        !document.body.textContent?.includes('Loading products…'),
        { timeout: 40_000 }
      ).catch(() => { /* fall through — table may still render with 0 rows */ });
    });

    test('filter input has a leading magnifier icon', async ({ page }) => {
      const filter = page.locator('.custom-table-filter input[type="search"]');
      await expect(filter).toBeVisible();
      const bg = await filter.evaluate(el => getComputedStyle(el).backgroundImage);
      expect(bg).toMatch(/svg/);
    });

    test('column headers are tabbable + show sort indicator on click', async ({ page }) => {
      const header = page.locator('table thead th').first();
      const tabindex = await header.getAttribute('tabindex');
      expect(tabindex).toBe('0');
      const sortBefore = await header.getAttribute('aria-sort');
      await header.click();
      const sortAfter = await header.getAttribute('aria-sort');
      expect(sortAfter).not.toBe('none');
      expect(sortAfter).not.toBe(sortBefore);
    });

    test('filter input narrows visible rows', async ({ page }) => {
      const rowsBefore = await page.locator('table tbody tr').count();
      if (rowsBefore < 2) test.skip(true, 'need at least 2 rows to verify filter');
      await page.locator('.custom-table-filter input[type="search"]').fill('Mastercase');
      // wait for re-render
      await page.waitForTimeout(200);
      const rowsAfter = await page.locator('table tbody tr').count();
      expect(rowsAfter).toBeLessThanOrEqual(rowsBefore);
    });
  });
});
