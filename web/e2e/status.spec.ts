import { expect, test } from '@playwright/test';

test('startsidan visar att webbappen når API:t och databasen', async ({ page }) => {
  await page.goto('/');
  await expect(page.getByRole('status')).toContainText('Ansluten');
});
