import { defineConfig } from 'vitest/config';

export default defineConfig({
  server: {
    port: 5173,
    strictPort: true,
    // API:t körs separat (se README.md). Samma ursprung i webbläsaren, ingen CORS behövs.
    proxy: { '/api': 'http://localhost:5080' },
  },
  test: {
    include: ['src/**/*.test.ts'],
  },
});
