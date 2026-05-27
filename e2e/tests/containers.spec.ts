import { test, expect } from '@playwright/test';

test.describe('Submission Requests page', () => {
  test.beforeEach(async ({ page }) => {
    await page.goto('/containers');
    await expect(page.getByRole('heading', { name: 'Submission Requests' })).toBeVisible();
    await page.waitForFunction(() => !!(window as any).Blazor);
    await page.waitForLoadState('networkidle');
  });

  test('shows requests list or empty state', async ({ page }) => {
    const table = page.locator('table').first();
    const empty = page.getByText(/No submission requests/);
    expect((await table.isVisible().catch(() => false)) || (await empty.isVisible())).toBeTruthy();
  });

  test('+ Submit Containers modal opens and closes via X', async ({ page }) => {
    await page.getByRole('button', { name: '+ Submit Containers' }).click();
    await expect(page.getByText('Submit Containers', { exact: true })).toBeVisible();
    await page.getByRole('button', { name: /close dialog/i }).first().click();
    await expect(page.getByText('Submit Containers', { exact: true })).toBeHidden();
  });

  test('Escape closes the Submit Containers modal', async ({ page }) => {
    await page.getByRole('button', { name: '+ Submit Containers' }).click();
    await expect(page.getByText('Submit Containers', { exact: true })).toBeVisible();
    await page.keyboard.press('Escape');
    await expect(page.getByText('Submit Containers', { exact: true })).toBeHidden();
  });

  test('Submit is disabled when no pairs are provided', async ({ page }) => {
    await page.getByRole('button', { name: '+ Submit Containers' }).click();
    await expect(page.getByRole('button', { name: 'Submit', exact: true })).toBeDisabled();
  });
});
