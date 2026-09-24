import { defineConfig, devices } from '@playwright/test';
import path from 'node:path';

// Isolated sandbox for the app under test: fake Firebot folder + its own usersettings.json.
export const SANDBOX = path.resolve(__dirname, '.sandbox');
export const FIREBOT_DIR = path.join(SANDBOX, 'firebot');
const PORT = Number(process.env.E2E_PORT ?? 5199);
const PROJECT = path.resolve(__dirname, '../FirebotGiveawayObsOverlay/FirebotGiveawayObsOverlay.WebApp');
// Test the published build — exactly what users run from the release ZIP.
const PUBLISH_DIR = path.resolve(__dirname, '.publish');

export default defineConfig({
  testDir: './tests',
  // One app instance with shared state (files + settings) → run serially.
  workers: 1,
  fullyParallel: false,
  forbidOnly: !!process.env.CI,
  retries: process.env.CI ? 1 : 0,
  timeout: 30_000,
  expect: { timeout: 7_500 },
  reporter: process.env.CI ? [['github'], ['html', { open: 'never' }]] : [['list'], ['html', { open: 'never' }]],
  globalSetup: './global-setup.ts',
  use: {
    baseURL: `http://127.0.0.1:${PORT}`,
    trace: 'retain-on-failure',
    screenshot: 'only-on-failure',
    video: 'retain-on-failure',
  },
  projects: [
    {
      name: 'chromium',
      use: {
        ...devices['Desktop Chrome'],
        // OBS browser sources are Chromium; allow using a preinstalled Chromium (e.g. PW_CHROMIUM_PATH=/opt/pw-browsers/chromium)
        launchOptions: process.env.PW_CHROMIUM_PATH ? { executablePath: process.env.PW_CHROMIUM_PATH } : {},
      },
    },
  ],
  webServer: {
    command: process.env.E2E_SERVER_COMMAND
      ?? `dotnet publish "${PROJECT}" -c Release -o "${PUBLISH_DIR}" --nologo -v q && dotnet "${path.join(PUBLISH_DIR, 'FirebotGiveawayObsOverlay.WebApp.dll')}"`,
    url: `http://127.0.0.1:${PORT}/giveaway`,
    reuseExistingServer: !process.env.CI,
    timeout: 180_000,
    stdout: 'pipe',
    stderr: 'pipe',
    env: {
      ASPNETCORE_URLS: `http://127.0.0.1:${PORT}`,
      ASPNETCORE_ENVIRONMENT: 'Production',
      DOTNET_CLI_TELEMETRY_OPTOUT: '1',
      LaunchBrowser: 'false',
      UserSettingsPath: path.join(SANDBOX, 'usersettings.json'),
      AppSettings__FireBotFileFolder: FIREBOT_DIR,
      AppSettings__CountdownHours: '0',
      AppSettings__CountdownMinutes: '1',
      AppSettings__CountdownSeconds: '30',
    },
  },
});
