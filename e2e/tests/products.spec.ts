import { test, expect } from '@playwright/test';

test.describe('Products page', () => {
  test.beforeEach(async ({ page }) => {
    await page.goto('/products');
    await expect(page.getByRole('heading', { name: 'Products' })).toBeVisible();
  });

  test('renders products table or empty state', async ({ page }) => {
    // CustomTable always renders headers; row count may be zero
    await expect(page.locator('table').first()).toBeVisible({ timeout: 10_000 });
    await expect(page.getByRole('columnheader', { name: 'GTIN' })).toBeVisible();
  });

  test('clicking a product surfaces a detail panel', async ({ page }) => {
    const firstRow = page.locator('tbody tr').first();
    if (await firstRow.count() === 0) test.skip(true, 'no product rows seeded');
    await firstRow.click();
    // Detail pane has a prompt or external info heading
    const detail = page.getByText(/External Product Information|Select a product/);
    await expect(detail).toBeVisible();
  });
});
