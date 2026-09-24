import fs from 'node:fs';
import path from 'node:path';
import { expect, type Page } from '@playwright/test';
import { FIREBOT_DIR, SANDBOX } from '../playwright.config';

type FirebotFile = 'prize.txt' | 'winner.txt' | 'giveaway.txt';

export function writeFirebot(file: FirebotFile, content: string) {
  fs.mkdirSync(FIREBOT_DIR, { recursive: true });
  fs.writeFileSync(path.join(FIREBOT_DIR, file), content);
}

export function clearFirebot() {
  for (const f of ['prize.txt', 'winner.txt', 'giveaway.txt']) {
    fs.rmSync(path.join(FIREBOT_DIR, f), { force: true });
  }
}

export function entries(n: number) {
  return Array.from({ length: n }, (_, i) => `User${i + 1}`).join('\r\n') + '\r\n';
}

export const userSettingsFile = path.join(SANDBOX, 'usersettings.json');

/** Waits until the page's Blazor Server circuit is live, so clicks/inputs reach the server. */
export async function waitForInteractive(page: Page) {
  await expect(page.locator('[data-interactive="true"], [data-overlay-ready="true"]').first()).toBeAttached({ timeout: 15_000 });
}

/** Parses "MM:SS" / "HH:MM:SS" / "SS" to seconds. */
export async function timerSeconds(page: Page): Promise<number> {
  const text = (await page.getByTestId('timer').innerText()).replace(/\s+/g, '');
  return text.split(':').map(Number).reduce((acc, v) => acc * 60 + v, 0);
}

/** Collects console errors / page errors so every test can assert a clean console. */
export function trackConsole(page: Page) {
  const errors: string[] = [];
  page.on('console', (msg) => { if (msg.type() === 'error') errors.push(msg.text()); });
  page.on('pageerror', (err) => errors.push(err.message));
  return {
    errors,
    expectClean: () => expect(errors, `console errors:\n${errors.join('\n')}`).toEqual([]),
  };
}
