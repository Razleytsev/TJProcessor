import { test, expect } from '@playwright/test';

async function waitForBlazor(page: any) {
  await page.waitForFunction(() => !!(window as any).Blazor);
  await page.waitForLoadState('networkidle');
}

test.describe('Keyboard navigation', () => {
  test('Tab order: brand → nav links → page actions', async ({ page }) => {
    await page.goto('/batches');
    await waitForBlazor(page);
    const seq: string[] = [];
    for (let i = 0; i < 8; i++) {
      await page.keyboard.press('Tab');
      const tag = await page.evaluate(() => {
        const el = document.activeElement as HTMLElement | null;
        return el ? `${el.tagName}:${(el.textContent ?? '').trim().substring(0, 30)}` : '';
      });
      seq.push(tag);
    }
    // First focused element should be the brand link
    expect(seq[0]).toContain('Abdulla');
    // The "+ Order Codes" button should appear in the sequence
    expect(seq.some(s => s.includes('Order Codes'))).toBeTruthy();
  });

  test('Enter activates focused brand link → navigates home', async ({ page }) => {
    await page.goto('/products');
    await waitForBlazor(page);
    await page.locator('.sidebar .brand').focus();
    await page.keyboard.press('Enter');
    await expect(page).toHaveURL(/\/batches$/);
  });

  test('Enter on sortable column header sorts (CustomTable)', async ({ page }) => {
    await page.goto('/products');
    await waitForBlazor(page);
    // Wait for table to render (external sync)
    await page.waitForFunction(
      () => !document.body.textContent?.includes('Loading products…'),
      { timeout: 40_000 }
    ).catch(() => {});
    const header = page.locator('table thead th').first();
    const before = await header.getAttribute('aria-sort');
    await header.focus();
    await page.keyboard.press('Enter');
    const after = await header.getAttribute('aria-sort');
    expect(after).not.toBe(before);
  });

  test('Escape closes any open modal globally', async ({ page }) => {
    await page.goto('/batches');
    await waitForBlazor(page);
    await page.getByRole('button', { name: '+ Order Codes' }).click();
    await expect(page.locator('.modal-dialog-box')).toBeVisible();
    await page.keyboard.press('Escape');
    await expect(page.locator('.modal-dialog-box')).toBeHidden();
  });

  test('focus ring visible on every interactive Tab-target', async ({ page }) => {
    await page.goto('/batches');
    await waitForBlazor(page);
    // Cycle Tab three times and inspect computed outline at each step
    for (let i = 0; i < 3; i++) {
      await page.keyboard.press('Tab');
      const outline = await page.evaluate(() => {
        const el = document.activeElement as HTMLElement | null;
        if (!el) return '';
        const cs = getComputedStyle(el);
        return `${cs.outlineStyle}|${cs.outlineWidth}|${cs.boxShadow}`;
      });
      // Either a real outline (non-"none") or a focus box-shadow
      expect(outline).not.toMatch(/^none\|0/);
    }
  });
});
