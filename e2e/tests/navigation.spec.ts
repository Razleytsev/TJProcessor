import { test, expect } from '@playwright/test';

test.describe('Navigation', () => {
  test('root redirects to /batches', async ({ page }) => {
    await page.goto('/');
    await expect(page).toHaveURL(/\/batches$/);
    await expect(page.getByRole('heading', { name: 'Order Batches' })).toBeVisible();
  });

  test('sidebar exposes all primary pages', async ({ page }) => {
    await page.goto('/batches');
    await expect(page.getByRole('link', { name: 'Order Batches' })).toBeVisible();
    await expect(page.getByRole('link', { name: 'Submission Requests' })).toBeVisible();
    await expect(page.getByRole('link', { name: /Product/ })).toBeVisible();
  });

  test('logo navigates back to home', async ({ page }) => {
    await page.goto('/containers');
    await page.getByRole('link', { name: 'Abdulla' }).first().click();
    await expect(page).toHaveURL(/\/(batches)?$/);
  });

  test('clicking a sidebar link changes route', async ({ page }) => {
    await page.goto('/batches');
    await page.getByRole('link', { name: 'Submission Requests' }).click();
    await expect(page).toHaveURL(/\/containers$/);
    await page.getByRole('link', { name: /Product/ }).click();
    await expect(page).toHaveURL(/\/products$/);
  });

  test('browser back restores prior page', async ({ page }) => {
    await page.goto('/batches');
    await page.getByRole('link', { name: 'Submission Requests' }).click();
    await expect(page).toHaveURL(/\/containers$/);
    await page.goBack();
    await expect(page).toHaveURL(/\/batches$/);
  });

  test('unknown route renders not-found view', async ({ page }) => {
    await page.goto('/this-route-does-not-exist');
    await expect(page.getByRole('alert')).toBeVisible();
  });
});
