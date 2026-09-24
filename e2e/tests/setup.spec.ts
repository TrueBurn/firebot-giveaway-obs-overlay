import fs from 'node:fs';
import { test, expect, type Page } from '@playwright/test';
import { FIREBOT_DIR } from '../playwright.config';
import { clearFirebot, timerSeconds, trackConsole, userSettingsFile, waitForInteractive, writeFirebot } from './helpers';

async function openSetup(page: Page) {
  await page.goto('/setup');
  await waitForInteractive(page);
}

/** A SliderSetting row by its exact label ("Prize", "Timer", "Prize Section Width", ...). */
function sliderRow(page: Page, label: string) {
  return page.locator('.slider-row').filter({
    has: page.locator('.slider-label > span:first-child', { hasText: new RegExp(`^${label}$`) }),
  });
}

async function openOverlay(page: Page) {
  await page.goto('/giveaway');
  await waitForInteractive(page);
  await expect(page.getByTestId('giveaway-container')).toBeVisible();
}

test.describe('Setup page (/setup)', () => {
  test.beforeEach(() => {
    clearFirebot();
    writeFirebot('prize.txt', 'PS5');
  });
  test.afterAll(() => clearFirebot());

  test('renders all sections with a clean console', async ({ page }) => {
    const console = trackConsole(page);
    await openSetup(page);
    await expect(page.getByRole('heading', { name: 'Overlay Setup' })).toBeVisible();
    await expect(page.getByText('Overlay Appearance')).toBeVisible();
    await expect(page.getByText('Giveaway Settings')).toBeVisible();
    await expect(page.getByText('Logging', { exact: true })).toBeVisible();
    await expect(page.getByText(/^v\d+\.\d+\.\d+/)).toBeVisible();
    console.expectClean();
  });

  test('theme change applies to an open overlay instantly and persists', async ({ browser }) => {
    const setup = await (await browser.newContext()).newPage();
    const overlay = await (await browser.newContext()).newPage();
    await openOverlay(overlay);
    await openSetup(setup);

    await setup.getByLabel('Theme').selectOption('Fire');
    await expect(overlay.getByTestId('giveaway-container')).toHaveAttribute('style', /--theme-primary: #ff4500/);

    await expect.poll(() => fs.existsSync(userSettingsFile) && fs.readFileSync(userSettingsFile, 'utf8'), { timeout: 5_000 })
      .toContain('"name": "Fire"');

    await setup.reload();
    await waitForInteractive(setup);
    await expect(setup.getByLabel('Theme')).toHaveValue('Fire');

    // restore
    await setup.getByLabel('Theme').selectOption('Warframe');
    await expect(overlay.getByTestId('giveaway-container')).toHaveAttribute('style', /--theme-primary: #00fff9/);
  });

  test('custom colours section appears only for the Custom theme', async ({ page }) => {
    await openSetup(page);
    await expect(page.getByText('Primary Color')).toHaveCount(0);
    await page.getByLabel('Theme').selectOption('Custom');
    await expect(page.getByText('Primary Color')).toBeVisible();
    await page.getByLabel('Theme').selectOption('Warframe');
    await expect(page.getByText('Primary Color')).toHaveCount(0);
  });

  test('prize width slider updates the label while dragging and the overlay on release', async ({ browser }) => {
    const setup = await (await browser.newContext()).newPage();
    const overlay = await (await browser.newContext()).newPage();
    await openOverlay(overlay);
    await openSetup(setup);

    const row = sliderRow(setup, 'Prize Section Width');
    const slider = row.locator('input[type=range]');
    await slider.fill('60'); // fires input + change, like a drag + release
    await expect(row.locator('.slider-value')).toHaveText('60%');
    await expect(overlay.getByTestId('giveaway-container')).toHaveAttribute('style', /--prize-width: 60%; --info-width: 40%/);

    await slider.fill('75');
    await expect(overlay.getByTestId('giveaway-container')).toHaveAttribute('style', /--prize-width: 75%/);
  });

  test('numeric input mode clamps out-of-range values', async ({ page }) => {
    await openSetup(page);
    const row = sliderRow(page, 'Prize');
    await row.getByRole('button', { name: 'Value' }).click();
    const input = row.locator('input[type=number]');
    await input.fill('99');
    await input.press('Enter');
    await expect(row.locator('.slider-value')).toHaveText('6.0 rem');

    await input.fill('3.5');
    await input.press('Enter');
    await expect(row.locator('.slider-value')).toHaveText('3.5 rem');
    await row.getByRole('button', { name: 'Slider' }).click();
    await expect(row.locator('input[type=range]')).toBeVisible();
  });

  test('font sizes are emitted with a dot decimal separator (culture-safe CSS)', async ({ browser }) => {
    const setup = await (await browser.newContext({ locale: 'de-DE' })).newPage();
    const overlay = await (await browser.newContext({ locale: 'de-DE' })).newPage();
    await openOverlay(overlay);
    await openSetup(setup);

    const row = sliderRow(setup, 'Timer');
    await row.locator('input[type=range]').fill('2.3');
    await expect(overlay.getByTestId('giveaway-container')).toHaveAttribute('style', /--timer-font-size: 2\.3rem/);
    await row.locator('input[type=range]').fill('3');
  });

  test('disabling the timer hides it on the overlay; enabling restarts it', async ({ browser }) => {
    const setup = await (await browser.newContext()).newPage();
    const overlay = await (await browser.newContext()).newPage();
    await openOverlay(overlay);
    await openSetup(setup);
    await expect(overlay.getByTestId('timer')).toBeVisible();

    const toggle = setup.locator('#timerEnabledCheck');
    await toggle.uncheck();
    await expect(setup.getByText('Disabled', { exact: true })).toBeVisible();
    await expect(overlay.getByTestId('timer')).toHaveCount(0);
    await expect(setup.getByLabel('Minutes')).toBeDisabled();
    await expect(setup.getByRole('button', { name: 'Reset' , exact: true })).toBeDisabled();

    await toggle.check();
    await expect(overlay.getByTestId('timer')).toBeVisible();
    await expect.poll(() => timerSeconds(overlay)).toBeGreaterThanOrEqual(88);
  });

  test('changing the duration and pressing Reset restarts the overlay countdown', async ({ browser }) => {
    const setup = await (await browser.newContext()).newPage();
    const overlay = await (await browser.newContext()).newPage();
    await openOverlay(overlay);
    await openSetup(setup);

    await setup.getByLabel('Minutes').fill('2');
    await setup.getByLabel('Seconds').fill('0');
    await setup.getByRole('button', { name: 'Reset', exact: true }).click();
    await expect(setup.getByText('Timer reset successfully!')).toBeVisible();
    await expect.poll(() => timerSeconds(overlay)).toBeGreaterThanOrEqual(118);

    // restore 1:30
    await setup.getByLabel('Minutes').fill('1');
    await setup.getByLabel('Seconds').fill('30');
    await setup.getByRole('button', { name: 'Reset', exact: true }).click();
    await expect.poll(() => timerSeconds(overlay)).toBeLessThanOrEqual(90);
  });

  test('editing the Firebot folder does not disturb the overlay until committed', async ({ browser }) => {
    const setup = await (await browser.newContext()).newPage();
    const overlay = await (await browser.newContext()).newPage();
    await openOverlay(overlay);
    await openSetup(setup);

    const folder = setup.getByLabel('Giveaway files folder');
    const original = await folder.inputValue();
    await folder.pressSequentially('-typing', { delay: 30 });
    // Mid-edit: overlay still reading the original folder
    await overlay.waitForTimeout(600);
    await expect(overlay.getByTestId('prize')).toHaveText('PS5');

    await folder.fill(original);
    await folder.press('Tab');
    await expect(overlay.getByTestId('prize')).toHaveText('PS5');
  });

  test('reset to defaults asks for confirmation and can be cancelled', async ({ page }) => {
    await openSetup(page);
    await page.getByLabel('Theme').selectOption('Neon');
    await expect(page.getByRole('button', { name: 'Reset to Defaults' })).toBeVisible();

    await page.getByRole('button', { name: 'Reset to Defaults' }).click();
    await page.getByRole('button', { name: 'Cancel' }).click();
    await expect(page.getByLabel('Theme')).toHaveValue('Neon');

    await page.getByRole('button', { name: 'Reset to Defaults' }).click();
    await page.getByRole('button', { name: 'Confirm Reset' }).click();
    await expect(page.getByLabel('Theme')).toHaveValue('Warframe');
    // Defaults come from appsettings.json/config, so the configured Firebot folder survives a reset
    await expect(page.getByLabel('Giveaway files folder')).toHaveValue(FIREBOT_DIR);
    await expect(page.getByText('Defaults', { exact: true })).toBeVisible();
    await expect(page.getByRole('button', { name: 'Reset to Defaults' })).toHaveCount(0);
    await expect.poll(() => fs.existsSync(userSettingsFile), { timeout: 3_000 }).toBe(false);
  });
});
