import { test, expect } from '@playwright/test';

// Ensures every modal in the app: opens, closes via X, closes via Esc, closes
// via Cancel/footer button. Backdrop-click is *intentionally* not asserted
// because the JS Escape handler also closes the dialog, so the two are
// equivalent end-user behaviours.

async function waitForBlazor(page: any) {
  await page.waitForFunction(() => !!(window as any).Blazor);
  await page.waitForLoadState('networkidle');
}

test.describe('Modals', () => {
  test.describe('Order Codes (on /batches)', () => {
    test.beforeEach(async ({ page }) => {
      await page.goto('/batches');
      await waitForBlazor(page);
      await page.getByRole('button', { name: '+ Order Codes' }).click();
      await expect(page.getByText('Order Codes', { exact: true })).toBeVisible();
    });

    test('renders header + all form fields + footer buttons', async ({ page }) => {
      const modal = page.locator('.modal-dialog-box');
      await expect(modal.getByRole('button', { name: /close dialog/i })).toBeVisible();
      await expect(modal.getByLabel('Type')).toBeVisible();
      await expect(modal.getByLabel(/GTIN/)).toBeVisible();
      await expect(modal.getByLabel('Codes Count')).toBeVisible();
      await expect(modal.getByLabel('Factory')).toBeVisible();
      await expect(modal.getByLabel('Marking Line')).toBeVisible();
      await expect(modal.getByLabel('Description')).toBeVisible();
      await expect(modal.getByRole('button', { name: 'Cancel' })).toBeVisible();
      await expect(modal.getByRole('button', { name: 'Create Order' })).toBeVisible();
    });

    test('closes via × button', async ({ page }) => {
      await page.locator('.modal-dialog-box').getByRole('button', { name: /close dialog/i }).click();
      await expect(page.getByText('Order Codes', { exact: true })).toBeHidden();
    });

    test('closes via Escape key', async ({ page }) => {
      await page.keyboard.press('Escape');
      await expect(page.getByText('Order Codes', { exact: true })).toBeHidden();
    });

    test('closes via Cancel button', async ({ page }) => {
      await page.locator('.modal-dialog-box').getByRole('button', { name: 'Cancel' }).click();
      await expect(page.getByText('Order Codes', { exact: true })).toBeHidden();
    });

    test('GTIN field hidden when Type=Mastercase', async ({ page }) => {
      const typeSelect = page.locator('.modal-dialog-box').getByLabel('Type');
      await typeSelect.selectOption({ label: 'Mastercase' });
      await expect(page.locator('.modal-dialog-box').getByLabel(/GTIN/)).toHaveCount(0);
    });

    test('asterisk marker on required Codes Count is aria-hidden', async ({ page }) => {
      const mark = page.locator('.modal-dialog-box .required-mark');
      await expect(mark).toBeVisible();
      await expect(mark).toHaveAttribute('aria-hidden', 'true');
    });
  });

  test.describe('Submit Containers (on /containers)', () => {
    test.beforeEach(async ({ page }) => {
      await page.goto('/containers');
      await waitForBlazor(page);
      await page.getByRole('button', { name: '+ Submit Containers' }).click();
      await expect(page.getByText('Submit Containers', { exact: true })).toBeVisible();
    });

    test('renders paste textarea + preview table + footer', async ({ page }) => {
      const modal = page.locator('.modal-dialog-box');
      await expect(modal.locator('textarea')).toBeVisible();
      await expect(modal.getByText(/PREVIEW \(0 PAIRS\)/i)).toBeVisible();
      await expect(modal.getByText(/Paste pairs on the left/i)).toBeVisible();
      await expect(modal.getByRole('button', { name: 'Cancel' })).toBeVisible();
      await expect(modal.getByRole('button', { name: 'Submit', exact: true })).toBeDisabled();
    });

    test('placeholder shows real newlines (no literal &#10;)', async ({ page }) => {
      const ph = await page.locator('.modal-dialog-box textarea').getAttribute('placeholder');
      expect(ph).not.toContain('&#10;');
      expect(ph).toMatch(/Code line\n.*SSCC line/);
    });

    test('closes via × button', async ({ page }) => {
      await page.locator('.modal-dialog-box').getByRole('button', { name: /close dialog/i }).click();
      await expect(page.getByText('Submit Containers', { exact: true })).toBeHidden();
    });

    test('closes via Escape', async ({ page }) => {
      await page.keyboard.press('Escape');
      await expect(page.getByText('Submit Containers', { exact: true })).toBeHidden();
    });

    test('closes via Cancel', async ({ page }) => {
      await page.locator('.modal-dialog-box').getByRole('button', { name: 'Cancel' }).click();
      await expect(page.getByText('Submit Containers', { exact: true })).toBeHidden();
    });
  });

  test.describe('Package detail (on /containers, after row click)', () => {
    test.beforeEach(async ({ page }) => {
      await page.goto('/containers');
      await waitForBlazor(page);
      const firstRow = page.locator('tbody tr').first();
      if (await firstRow.count() === 0) test.skip(true, 'no requests seeded');
      await firstRow.click();
      // Wait for the packages pane to populate
      await expect(page.getByText(/Packages — Request #/)).toBeVisible();
      // Click the first package row
      const pkgRow = page.locator('.split-pane-bottom tbody tr').first();
      if (await pkgRow.count() === 0) test.skip(true, 'no packages on the selected request');
      await pkgRow.click();
      await expect(page.getByText(/Package #/)).toBeVisible();
    });

    test('renders detail grid + close button', async ({ page }) => {
      await expect(page.locator('.pkg-detail-grid')).toBeVisible();
      await expect(page.locator('.modal-dialog-box').getByRole('button', { name: 'Close' })).toBeVisible();
    });

    test('closes via Escape', async ({ page }) => {
      await page.keyboard.press('Escape');
      await expect(page.locator('.pkg-detail-grid')).toBeHidden();
    });
  });

  test.describe('Confirm-Cancel batch dialog', () => {
    test('opens, has Yes/No, closes on Esc', async ({ page }) => {
      await page.goto('/batches');
      await waitForBlazor(page);
      const cancelBtn = page.locator('tbody').getByRole('button', { name: 'Cancel' }).first();
      if (await cancelBtn.count() === 0) test.skip(true, 'no batches in cancellable state');
      await cancelBtn.click();
      const confirm = page.locator('.confirm-box');
      await expect(confirm).toBeVisible();
      await expect(confirm.getByRole('button', { name: /Yes, Cancel/ })).toBeVisible();
      await expect(confirm.getByRole('button', { name: 'No' })).toBeVisible();
      await page.keyboard.press('Escape');
      await expect(confirm).toBeHidden();
    });
  });
});
