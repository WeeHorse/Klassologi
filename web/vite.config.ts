import { defineConfig } from 'vitest/config';

// Playwright kör egna instanser på andra portar (se playwright.config.ts), så att utvecklingsservrarna inte störs.
const apiUrl = process.env.KLASSOLOGI_API_URL ?? 'http://localhost:5080';

export default defineConfig({
  server: {
    port: Number(process.env.KLASSOLOGI_WEB_PORT ?? 5173),
    strictPort: true,
    // API:t körs separat (se README.md). Samma ursprung i webbläsaren, så sessionscookien fungerar och ingen CORS behövs.
    proxy: { '/api': apiUrl },
  },
  test: {
    include: ['src/**/*.test.ts'],
  },
});
