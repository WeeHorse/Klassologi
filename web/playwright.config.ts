import { defineConfig, devices } from '@playwright/test';

// Kräver att databasen är igång (docker compose up -d) och att API:ts anslutningssträng är satt (se README.md).
export default defineConfig({
  testDir: 'e2e',
  use: { baseURL: 'http://localhost:5173' },
  projects: [{ name: 'chromium', use: { ...devices['Desktop Chrome'] } }],
  webServer: [
    {
      command: 'dotnet run --project ../src/Klassologi.Api --launch-profile http',
      url: 'http://localhost:5080/api/status',
      reuseExistingServer: !process.env.CI,
      timeout: 120_000,
    },
    {
      command: 'npm run dev',
      url: 'http://localhost:5173',
      reuseExistingServer: !process.env.CI,
    },
  ],
});
