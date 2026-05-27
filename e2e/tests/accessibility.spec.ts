import { test, expect } from '@playwright/test';

test.describe('Accessibility & UX conventions', () => {
  test('document has a <main> landmark', async ({ page }) => {
    await page.goto('/batches');
    await expect(page.locator('main')).toBeVisible();
  });

  test('document has a <nav> landmark', async ({ page }) => {
    await page.goto('/batches');
    await expect(page.locator('nav').first()).toBeVisible();
  });

  test('logo is keyboard activatable', async ({ page }) => {
    await page.goto('/products');
    const brand = page.getByRole('link', { name: 'Abdulla' }).first();
    await brand.focus();
    await page.keyboard.press('Enter');
    await expect(page).toHaveURL(/\/(batches)?$/);
  });

  test('focused interactive elements show a visible outline', async ({ page }) => {
    await page.goto('/batches');
    await page.keyboard.press('Tab');
    const outline = await page.evaluate(() => {
      const el = document.activeElement as HTMLElement | null;
      if (!el) return '';
      const cs = getComputedStyle(el);
      return `${cs.outlineStyle}|${cs.outlineColor}|${cs.outlineWidth}|${cs.boxShadow}`;
    });
    // Either a real outline OR a focus box-shadow should be present
    expect(outline).not.toMatch(/^none\|/);
  });

  test('Order Codes modal traps Escape and dismisses cleanly', async ({ page }) => {
    await page.goto('/batches');
    await page.waitForFunction(() => !!(window as any).Blazor);
    await page.waitForLoadState('networkidle');
    await page.getByRole('button', { name: '+ Order Codes' }).click();
    await expect(page.getByText('Order Codes', { exact: true })).toBeVisible();
    await page.keyboard.press('Escape');
    await expect(page.getByText('Order Codes', { exact: true })).toBeHidden();
  });

  test('icon-only download button exposes an accessible name', async ({ page }) => {
    await page.goto('/batches');
    const dl = page.getByRole('button', { name: /Download|⬇/ }).first();
    if (await dl.count() === 0) test.skip(true, 'no download buttons rendered');
    const name = (await dl.getAttribute('aria-label')) ?? (await dl.innerText());
    expect(name.trim().length).toBeGreaterThan(0);
  });
});
