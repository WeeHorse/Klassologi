import { execSync } from 'node:child_process';
import { expect, test as setup } from '@playwright/test';
import { activate, adminState, run } from './helpers';

// Q15: den första administratören skapas med kommandot på servern och aktiverar sitt konto via länken.
setup('administratör via kommandot create-admin-invitation', async ({ page }) => {
  const output = execSync('dotnet run --project ../src/Klassologi.Api -- create-admin-invitation', { encoding: 'utf8' });
  const path = output.match(/\/aktivera#[A-Za-z0-9_-]+/)?.[0];
  expect(path, 'Kommandot skrev inte ut någon länk.').toBeTruthy();

  await page.goto(path!);
  await expect(page.getByText('Du är inbjuden som administratör')).toBeVisible();
  await activate(page, path!, 'Ada Admin', `admin-${run}@example.se`);

  await expect(page.getByRole('heading', { name: 'Mina klasser' })).toBeVisible();
  await page.context().storageState({ path: adminState });
});
