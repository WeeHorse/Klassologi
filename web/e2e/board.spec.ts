import { expect, test, type Browser, type Page } from '@playwright/test';
import { activate, adminState, expectAccessible, run, sharedLink } from './helpers';

/** Steg 4 från början till slut, härlett från Specifications/UseCases.md: UC-08, UC-09, UC-10 och fästning. */
test.describe.configure({ mode: 'serial' });

const className = `Anslagstavla ${run}`;
let classPath = '';
let teacher: Page;
let representative: Page;
let student: Page;

async function newPage(browser: Browser): Promise<Page> {
  const page = await (await browser.newContext()).newPage();
  // Ett inlägg får aldrig kunna köra skript, t.ex. alert() från användarens text.
  page.on('dialog', (dialog) => {
    throw new Error(`Oväntad dialog: ${dialog.message()}`);
  });
  return page;
}

async function invite(role: 'Elev' | 'Elevrepresentant'): Promise<string> {
  await teacher.goto(classPath);
  // Ionics yttre element tar emot pekarklick, så rollen väljs med tangentbordet.
  await teacher.getByRole('tab', { name: role, exact: true }).press('Enter');
  await expect(teacher.getByRole('tab', { name: role, exact: true })).toHaveAttribute('aria-selected', 'true');
  await teacher.getByRole('button', { name: 'Skapa inbjudningslänk' }).click();
  return sharedLink(teacher);
}

async function publish(page: Page, title: string, body: string): Promise<void> {
  await page.getByText('Nytt inlägg').click();
  await page.getByLabel('Rubrik').fill(title);
  await page.getByRole('textbox', { name: 'Text', exact: true }).fill(body);
  await page.getByRole('button', { name: 'Publicera' }).click();
  await expect(page.getByRole('heading', { name: title, level: 3 })).toBeVisible();
}

const post = (page: Page, title: string) => page.getByRole('article', { name: title });

test('klassen med lärare, elevrepresentant och elev', async ({ browser }) => {
  const admin = await (await browser.newContext({ storageState: adminState })).newPage();
  await admin.goto('/');
  await admin.getByLabel('Klassens namn').fill(className);
  await admin.getByRole('button', { name: 'Skapa klass' }).click();
  const teacherLink = await sharedLink(admin);

  teacher = await newPage(browser);
  await activate(teacher, teacherLink, 'Tove Lärare', `tove-${run}@example.se`);
  await expect(teacher.getByText('Din roll: Lärare')).toBeVisible();
  classPath = new URL(teacher.url()).pathname;

  representative = await newPage(browser);
  await activate(representative, await invite('Elevrepresentant'), 'Rut Representant', `rut-${run}@example.se`);
  student = await newPage(browser);
  await activate(student, await invite('Elev'), 'Elsa Elev', `elsa-${run}@example.se`);

  await expect(student.getByText('Här finns inga inlägg än.')).toBeVisible();
});

test('läraren förhandsgranskar och publicerar ett inlägg i Markdown, utan att HTML körs', async () => {
  await teacher.goto(classPath);
  await teacher.getByText('Nytt inlägg').click();
  await teacher.getByLabel('Rubrik').fill('Schema vecka 48');
  await teacher.getByRole('textbox', { name: 'Text', exact: true }).fill(
    '**Viktigt:** ny sal.\n\n- Matte i A1\n- [Schemat](https://example.se/schema)\n\n<img src=x onerror="alert(1)"> [klick](javascript:alert(1))',
  );
  await teacher.getByRole('button', { name: 'Förhandsgranska' }).click();
  await expect(teacher.getByLabel('Förhandsgranskning').locator('strong')).toHaveText('Viktigt:');

  await teacher.getByRole('button', { name: 'Publicera' }).click();

  const article = post(teacher, 'Schema vecka 48');
  await expect(article.locator('strong')).toHaveText('Viktigt:');
  await expect(article.getByRole('link', { name: 'Schemat' })).toHaveAttribute('href', 'https://example.se/schema');
  await expect(article.getByRole('link', { name: 'klick' })).toHaveCount(0);
  await expect(article.locator('img')).toHaveCount(0);
  await expect(article).toContainText('<img src=x onerror="alert(1)">');
  await expect(article).toContainText('Tove Lärare · Lärare');
  await expectAccessible(teacher);
});

test('elevrepresentanten publicerar, eleven läser men kan inte publicera', async () => {
  await representative.goto(classPath);
  await publish(representative, 'Klassfest fredag', 'Anmäl dig till *Rut*.');

  await student.goto(classPath);
  await expect(post(student, 'Klassfest fredag')).toBeVisible();
  await expect(post(student, 'Schema vecka 48')).toBeVisible();
  await expect(student.getByText('Nytt inlägg')).toHaveCount(0);
  await expect(student.getByRole('button', { name: /^Redigera/ })).toHaveCount(0);
  await expect(student.getByRole('button', { name: /^Fäst/ })).toHaveCount(0);
  await expectAccessible(student);
});

test('läraren fäster elevrepresentantens inlägg överst', async () => {
  await teacher.goto(classPath);
  await expect(teacher.getByRole('article').first()).toHaveAccessibleName('Klassfest fredag');

  await post(teacher, 'Schema vecka 48').getByRole('button', { name: 'Fäst Schema vecka 48' }).click();

  await expect(teacher.getByRole('article').first()).toHaveAccessibleName('Schema vecka 48');
  await expect(post(teacher, 'Schema vecka 48')).toContainText('Fäst');
  await expect(post(teacher, 'Schema vecka 48').getByRole('button', { name: 'Lossa Schema vecka 48' })).toBeVisible();
});

test('författaren redigerar sitt inlägg, men läraren kan inte redigera andras', async () => {
  await expect(post(teacher, 'Klassfest fredag').getByRole('button', { name: /^Redigera/ })).toHaveCount(0);

  await representative.goto(classPath);
  await post(representative, 'Klassfest fredag').getByRole('button', { name: 'Redigera Klassfest fredag' }).click();
  const editing = representative.getByRole('article', { name: 'Redigera "Klassfest fredag"' });
  await editing.getByLabel('Rubrik').fill('Klassfest lördag');
  await editing.getByRole('button', { name: 'Spara ändringar' }).click();

  await expect(post(representative, 'Klassfest lördag')).toContainText('redigerad');
});

test('författaren tar bort sitt inlägg efter bekräftelse, och administratören ser det borttagna', async ({ browser }) => {
  await post(representative, 'Klassfest lördag').getByRole('button', { name: 'Ta bort Klassfest lördag' }).click();
  await expect(representative.getByRole('alertdialog')).toContainText('Ta bort inlägget?');
  await representative.getByRole('button', { name: 'Avbryt' }).click();
  await expect(post(representative, 'Klassfest lördag')).toBeVisible();

  await post(representative, 'Klassfest lördag').getByRole('button', { name: 'Ta bort Klassfest lördag' }).click();
  await representative.getByRole('alertdialog').getByRole('button', { name: 'Ta bort' }).click();
  await expect(post(representative, 'Klassfest lördag')).toHaveCount(0);

  await student.goto(classPath);
  await expect(post(student, 'Klassfest lördag')).toHaveCount(0);

  const admin = await (await browser.newContext({ storageState: adminState })).newPage();
  await admin.goto(classPath);
  await expect(post(admin, 'Klassfest lördag')).toContainText('Borttaget av Rut Representant');
  await expect(post(admin, 'Klassfest lördag')).toContainText('Raderas permanent');
  await expectAccessible(admin);
});
