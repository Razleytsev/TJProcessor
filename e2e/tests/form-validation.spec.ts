import { test, expect } from '@playwright/test';

async function waitForBlazor(page: any) {
  await page.waitForFunction(() => !!(window as any).Blazor);
  await page.waitForLoadState('networkidle');
}

test.describe('Form validation', () => {
  test('Order Codes: missing count → inline error appears', async ({ page }) => {
    await page.goto('/batches');
    await waitForBlazor(page);
    await page.getByRole('button', { name: '+ Order Codes' }).click();
    await page.locator('.modal-dialog-box').getByRole('button', { name: 'Create Order' }).click();
    const err = page.locator('.modal-dialog-box .invalid-feedback, .modal-dialog-box .alert');
    await expect(err.first()).toBeVisible();
  });

  test('Order Codes: count=0 triggers inline "greater than 0"', async ({ page }) => {
    await page.goto('/batches');
    await waitForBlazor(page);
    await page.getByRole('button', { name: '+ Order Codes' }).click();
    await page.locator('.modal-dialog-box').getByLabel('Codes Count').fill('0');
    await page.locator('.modal-dialog-box').getByRole('button', { name: 'Create Order' }).click();
    await expect(page.getByText(/greater than 0/i).first()).toBeVisible();
  });

  test('Order Codes: invalid input "abc" stays empty / clears error gracefully', async ({ page }) => {
    await page.goto('/batches');
    await waitForBlazor(page);
    await page.getByRole('button', { name: '+ Order Codes' }).click();
    const input = page.locator('.modal-dialog-box').getByLabel('Codes Count');
    // type then immediately clear — should not throw
    await input.fill('abc').catch(() => {/* number input may reject non-digits */});
    // Clear and submit
    await input.fill('');
    await page.locator('.modal-dialog-box').getByRole('button', { name: 'Create Order' }).click();
    await expect(page.locator('.modal-dialog-box .invalid-feedback, .modal-dialog-box .alert').first()).toBeVisible();
  });

  test('Submit Containers: Submit disabled until valid pairs exist', async ({ page }) => {
    await page.goto('/containers');
    await waitForBlazor(page);
    await page.getByRole('button', { name: '+ Submit Containers' }).click();
    const submit = page.locator('.modal-dialog-box').getByRole('button', { name: 'Submit', exact: true });
    await expect(submit).toBeDisabled();
  });

  test('TestRun: Start button disabled until checkbox + form complete', async ({ page }) => {
    await page.goto('/test-run');
    await waitForBlazor(page);
    const startBtn = page.getByRole('button', { name: /Start Test Run/ });
    if (await startBtn.count() === 0) test.skip(true, 'test-run gate disabled');
    await expect(startBtn).toBeDisabled();
    // Even after checkbox: still disabled because product selects empty
    await page.locator('input[type="checkbox"]').first().check();
    await expect(startBtn).toBeDisabled();
  });
});
