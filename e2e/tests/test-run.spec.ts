import { test, expect } from '@playwright/test';

test.describe('Test Run page', () => {
  test('renders either the gate banner or the form', async ({ page }) => {
    await page.goto('/test-run');
    const formHeader = page.getByRole('heading', { name: 'Production Contour Test Run' });
    const disabledBanner = page.getByText(/Test Run is disabled/);
    const oneVisible = (await formHeader.isVisible().catch(() => false)) ||
      (await disabledBanner.isVisible().catch(() => false));
    expect(oneVisible).toBeTruthy();
  });

  test('Start button is disabled until the confirm checkbox is checked', async ({ page }) => {
    await page.goto('/test-run');
    const startBtn = page.getByRole('button', { name: /Start Test Run/ });
    if (await startBtn.count() === 0) test.skip(true, 'test run gate is disabled');
    await expect(startBtn).toBeDisabled();
  });
});
