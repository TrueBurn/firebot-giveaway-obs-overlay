import { test, expect } from '@playwright/test';
import { trackConsole } from './helpers';

test('home page renders and navigates to setup', async ({ page }) => {
  const console = trackConsole(page);
  await page.goto('/');
  await expect(page.locator('h1, h2, h3').first()).toBeVisible();
  await page.getByRole('link', { name: /setup/i }).first().click();
  await expect(page).toHaveURL(/\/setup$/);
  await expect(page.getByRole('heading', { name: 'Overlay Setup' })).toBeVisible();
  console.expectClean();
});

test('static assets are served with caching headers', async ({ request }) => {
  const res = await request.get('/giveaway.css');
  expect(res.ok()).toBe(true);
  expect(res.headers()['cache-control'] ?? '').not.toBe('');
  const font = await request.get('/fonts/orbitron-latin-600-normal.woff2');
  expect(font.ok()).toBe(true);
});
