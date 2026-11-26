import { defineConfig, devices } from '@playwright/test';

// E2E-testerna kör egna instanser av API och webbapp på andra portar än utvecklingsservrarna, med hög gräns för
// anropstakt. De använder utvecklingsdatabasen (docker compose up -d) och API:ts anslutningssträng (se README.md).
const apiPort = 5081;
const webPort = 5174;

export default defineConfig({
  testDir: 'e2e',
  use: { baseURL: `http://localhost:${webPort}` },
  projects: [
    { name: 'setup', testMatch: /.*\.setup\.ts/ },
    { name: 'chromium', use: { ...devices['Desktop Chrome'] }, dependencies: ['setup'] },
  ],
  webServer: [
    {
      command: `dotnet run --project ../src/Klassologi.Api --launch-profile http -- --urls http://localhost:${apiPort}`,
      url: `http://localhost:${apiPort}/api/status`,
      env: { Auth__RateLimitPerMinute: '10000' },
      reuseExistingServer: !process.env.CI,
      timeout: 120_000,
    },
    {
      command: 'npm run dev -- --bypass-lock',
      url: `http://localhost:${webPort}`,
      env: { KLASSOLOGI_API_URL: `http://localhost:${apiPort}`, KLASSOLOGI_WEB_PORT: String(webPort) },
      reuseExistingServer: !process.env.CI,
    },
  ],
});
