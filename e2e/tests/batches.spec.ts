import { test, expect } from '@playwright/test';

test.describe('Order Batches page', () => {
  test.beforeEach(async ({ page }) => {
    await page.goto('/batches');
    await expect(page.getByRole('heading', { name: 'Order Batches' })).toBeVisible();
  });

  test('shows batch table with at least one row or an empty state', async ({ page }) => {
    const table = page.locator('table').first();
    const tableVisible = await table.isVisible().catch(() => false);
    const empty = page.getByText(/No batches/);
    expect(tableVisible || (await empty.isVisible())).toBeTruthy();
  });

  test('opening + Order Codes modal renders all form fields', async ({ page }) => {
    await page.getByRole('button', { name: '+ Order Codes' }).click();
    await expect(page.getByText('Order Codes', { exact: true })).toBeVisible();
    await expect(page.getByLabel('Codes Count')).toBeVisible();
    await expect(page.getByLabel('Description')).toBeVisible();
    await expect(page.getByRole('button', { name: 'Create Order' })).toBeVisible();
  });

  test('Escape closes the Order Codes modal', async ({ page }) => {
    await page.getByRole('button', { name: '+ Order Codes' }).click();
    await expect(page.getByText('Order Codes', { exact: true })).toBeVisible();
    await page.keyboard.press('Escape');
    await expect(page.getByText('Order Codes', { exact: true })).toBeHidden();
  });

  test('Cancel button closes the modal', async ({ page }) => {
    await page.getByRole('button', { name: '+ Order Codes' }).click();
    await page.getByRole('button', { name: 'Cancel' }).click();
    await expect(page.getByText('Order Codes', { exact: true })).toBeHidden();
  });

  test('selecting a batch row shows the orders pane', async ({ page }) => {
    const firstRow = page.locator('tbody tr').first();
    if (await firstRow.count() === 0) {
      test.skip(true, 'no batches available in seeded data');
    }
    await firstRow.click();
    await expect(page.getByText(/Orders\s*[—–-]\s*Batch #/)).toBeVisible();
  });

  test('codes count input rejects zero', async ({ page }) => {
    await page.getByRole('button', { name: '+ Order Codes' }).click();
    const input = page.getByLabel('Codes Count');
    await input.fill('0');
    await page.getByRole('button', { name: 'Create Order' }).click();
    await expect(page.getByText(/greater than 0|Codes count/i)).toBeVisible();
  });
});
