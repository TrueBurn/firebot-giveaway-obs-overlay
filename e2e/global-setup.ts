import fs from 'node:fs';
import { SANDBOX, FIREBOT_DIR } from './playwright.config';

export default function globalSetup() {
  // Note: runs before webServer starts, so the app always boots with a clean sandbox.
  fs.rmSync(SANDBOX, { recursive: true, force: true });
  fs.mkdirSync(FIREBOT_DIR, { recursive: true });
}
