import AxeBuilder from '@axe-core/playwright';
import { expect, type Page } from '@playwright/test';

export const adminState = 'playwright/.auth/admin.json';
export const password = 'ett-långt-lösenord';
export const run = Date.now().toString(36);

/** WCAG 2.2 AA enligt Q17, kontrollerat med axe. */
export async function expectAccessible(page: Page): Promise<void> {
  const results = await new AxeBuilder({ page })
    .withTags(['wcag2a', 'wcag2aa', 'wcag21a', 'wcag21aa', 'wcag22aa'])
    .analyze();
  const summary = results.violations.map((v) => `${v.id}: ${v.help} (${v.nodes.map((n) => n.target.join(' ')).join(', ')})`);
  expect(summary).toEqual([]);
}

export async function sharedLink(page: Page): Promise<string> {
  const link = (await page.getByTestId('share-link').last().textContent())?.trim();
  expect(link).toMatch(/^http:\/\/localhost:\d+\/(aktivera|aterstall)#[A-Za-z0-9_-]{40,}$/);
  return link!;
}

export async function activate(page: Page, link: string, name: string, email: string): Promise<void> {
  await page.goto(link);
  await page.getByLabel('Ditt namn').fill(name);
  await page.getByLabel('E-postadress').fill(email);
  await page.getByLabel('Välj lösenord').fill(password);
  await page.getByRole('button', { name: 'Skapa konto' }).click();
}
