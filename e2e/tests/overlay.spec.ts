import { test, expect } from '@playwright/test';
import { clearFirebot, entries, timerSeconds, trackConsole, waitForInteractive, writeFirebot } from './helpers';

test.describe('OBS overlay (/giveaway)', () => {
  test.beforeEach(() => clearFirebot());
  test.afterAll(() => clearFirebot());

  test('is fully transparent/empty when no giveaway is running', async ({ page }) => {
    const console = trackConsole(page);
    await page.goto('/giveaway');
    await waitForInteractive(page);

    await expect(page.getByTestId('giveaway-container')).toHaveCount(0);
    // Nothing from Blazor's error / reconnect UI may ever be visible on stream
    await expect(page.locator('#blazor-error-ui')).toBeHidden();
    await expect(page.locator('#components-reconnect-modal')).toBeHidden();
    console.expectClean();
  });

  test('shows prize, entry count and countdown as soon as Firebot writes the files', async ({ page }) => {
    const console = trackConsole(page);
    await page.goto('/giveaway');
    await waitForInteractive(page);

    writeFirebot('giveaway.txt', entries(3));
    writeFirebot('prize.txt', 'Gaming Keyboard\r\n');

    await expect(page.getByTestId('prize')).toHaveText('Gaming Keyboard');
    await expect(page.getByTestId('entry-count')).toHaveText('3');
    await expect(page.getByTestId('timer')).toHaveText(/^\s*0?1\s*:\s*[0-5]\d\s*$/);
    console.expectClean();
  });

  test('entry count updates live without reloading, and ignores blank lines', async ({ page }) => {
    writeFirebot('prize.txt', 'PS5');
    writeFirebot('giveaway.txt', entries(1));
    await page.goto('/giveaway');
    await waitForInteractive(page);
    await expect(page.getByTestId('entry-count')).toHaveText('1');

    writeFirebot('giveaway.txt', 'A\n\nB\r\n\r\nC\n');
    await expect(page.getByTestId('entry-count')).toHaveText('3');
    await expect(page.getByTestId('entry-count')).toHaveClass(/entry-count-animate/);
  });

  test('countdown ticks down once per second', async ({ page }) => {
    writeFirebot('prize.txt', 'PS5');
    await page.goto('/giveaway');
    await waitForInteractive(page);
    await expect(page.getByTestId('timer')).toBeVisible();

    const first = await timerSeconds(page);
    await expect.poll(() => timerSeconds(page), { timeout: 5_000 }).toBeLessThanOrEqual(first - 2);
  });

  test('countdown survives an OBS browser-source reload (no restart from full)', async ({ page }) => {
    writeFirebot('prize.txt', 'PS5');
    await page.goto('/giveaway');
    await waitForInteractive(page);
    await expect(page.getByTestId('timer')).toBeVisible();
    await page.waitForTimeout(3_000);
    const before = await timerSeconds(page);

    await page.reload();
    await waitForInteractive(page);
    const after = await timerSeconds(page);
    expect(after).toBeLessThanOrEqual(before);
    expect(after).toBeLessThan(90); // configured duration is 1:30
  });

  test('multiple overlays stay in sync', async ({ browser }) => {
    writeFirebot('prize.txt', 'PS5');
    const a = await (await browser.newContext()).newPage();
    const b = await (await browser.newContext()).newPage();
    await a.goto('/giveaway');
    await waitForInteractive(a);
    await a.waitForTimeout(2_000);
    await b.goto('/giveaway');
    await waitForInteractive(b);

    await expect.poll(async () => Math.abs((await timerSeconds(a)) - (await timerSeconds(b)))).toBeLessThanOrEqual(1);
  });

  test('winner announcement appears, pauses the timer, and clears when winner.txt is emptied', async ({ page }) => {
    writeFirebot('prize.txt', 'PS5');
    writeFirebot('giveaway.txt', entries(5));
    await page.goto('/giveaway');
    await waitForInteractive(page);
    await expect(page.getByTestId('timer')).toBeVisible();

    writeFirebot('winner.txt', 'CoolStreamer42');
    await expect(page.getByTestId('winner-overlay')).toBeVisible();
    await expect(page.getByTestId('winner')).toHaveText('CoolStreamer42');
    await expect(page.getByTestId('giveaway-container')).toHaveClass(/has-winner/);

    const paused = await timerSeconds(page);
    await page.waitForTimeout(2_200);
    expect(await timerSeconds(page)).toBe(paused);

    writeFirebot('winner.txt', '');
    await expect(page.getByTestId('winner-overlay')).toHaveCount(0);
  });

  test('overlay disappears when the giveaway ends (prize.txt removed)', async ({ page }) => {
    writeFirebot('prize.txt', 'PS5');
    await page.goto('/giveaway');
    await waitForInteractive(page);
    await expect(page.getByTestId('giveaway-container')).toBeVisible();

    clearFirebot();
    await expect(page.getByTestId('giveaway-container')).toHaveCount(0);
  });

  test('loads no third-party resources (works offline in OBS)', async ({ page }) => {
    const external: string[] = [];
    page.on('request', (req) => {
      const url = new URL(req.url());
      if (!['127.0.0.1', 'localhost'].includes(url.hostname) && url.protocol.startsWith('http')) external.push(req.url());
    });
    writeFirebot('prize.txt', 'PS5');
    await page.goto('/giveaway');
    await waitForInteractive(page);
    await expect(page.getByTestId('prize')).toBeVisible();
    await page.evaluate(() => document.fonts.ready);

    expect(external).toEqual([]);
    expect(await page.evaluate(() => document.fonts.check('600 16px Orbitron'))).toBe(true);
  });

  test('matches the recommended 1200x300 browser-source size', async ({ page }) => {
    await page.setViewportSize({ width: 1200, height: 300 });
    writeFirebot('prize.txt', 'A very long prize name that should wrap nicely inside the box');
    writeFirebot('giveaway.txt', entries(1234));
    await page.goto('/giveaway');
    await waitForInteractive(page);
    await expect(page.getByTestId('entry-count')).toHaveText('1234');

    const box = await page.getByTestId('giveaway-container').boundingBox();
    expect(box).not.toBeNull();
    expect(box!.width).toBeLessThanOrEqual(1200);
    expect(box!.height).toBeLessThanOrEqual(300);
    // No scrollbars in the browser source
    const overflow = await page.evaluate(() => ({
      x: document.documentElement.scrollWidth > window.innerWidth,
      y: document.documentElement.scrollHeight > window.innerHeight,
    }));
    expect(overflow).toEqual({ x: false, y: false });
  });
});
