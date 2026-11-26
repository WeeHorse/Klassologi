import { expect, test } from '@playwright/test';

test('statussidan visar att webbappen når API:t och databasen', async ({ page }) => {
  await page.goto('/status');
  await expect(page.getByRole('status')).toContainText('Ansluten');
});
