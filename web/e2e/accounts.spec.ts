import { expect, test, type Browser, type Page } from '@playwright/test';
import { activate, adminState, expectAccessible, password, run, sharedLink } from './helpers';

/**
 * Steg 3 från början till slut, härlett från Specifications/UseCases.md: UC-01 (ändrad), UC-02, UC-04, UC-05,
 * medlemslista, ändring av roll och återställning av lösenord. Varje vy kontrolleras mot WCAG 2.2 AA.
 */
test.describe.configure({ mode: 'serial' });

const className = `Åk 2 – Systemutveckling ${run}`;
const studentEmail = `elev-${run}@example.se`;
let teacherLink = '';
let studentLink = '';
let classPath = '';
let teacher: Page;

async function newPage(browser: Browser): Promise<Page> {
  return (await browser.newContext()).newPage();
}

test('administratören skapar en klass och får en lärarinbjudan', async ({ browser }) => {
  const page = await (await browser.newContext({ storageState: adminState })).newPage();
  await page.goto('/');
  await expect(page.getByRole('heading', { name: 'Mina klasser' })).toBeVisible();
  await expectAccessible(page);

  await page.getByLabel('Klassens namn').fill(className);
  await page.getByRole('button', { name: 'Skapa klass' }).click();

  await expect(page.getByRole('status')).toContainText(`Klassen ${className} är skapad`);
  teacherLink = await sharedLink(page);
  await expect(page.getByText('Länken kan bara användas en gång')).toBeVisible();
  await expectAccessible(page);
});

test('läraren aktiverar sitt konto och kommer till klassen', async ({ browser }) => {
  teacher = await newPage(browser);
  await teacher.goto(teacherLink);
  await expect(teacher.getByText(`Du är inbjuden till ${className} som Lärare`)).toBeVisible();
  await expectAccessible(teacher);

  await activate(teacher, teacherLink, 'Lena Lärare', `larare-${run}@example.se`);

  await expect(teacher.getByRole('heading', { name: className })).toBeVisible();
  await expect(teacher.getByText('Din roll: Lärare')).toBeVisible();
  classPath = new URL(teacher.url()).pathname;
  expect(classPath).toMatch(/^\/klasser\/[0-9a-f-]{36}$/);
  await expectAccessible(teacher);
});

test('läraren bjuder in en elev, som aktiverar sitt konto', async ({ browser }) => {
  await teacher.getByRole('button', { name: 'Skapa inbjudningslänk' }).click();
  studentLink = await sharedLink(teacher);
  await expect(teacher.getByText('Inbjudan som elev')).toBeVisible();

  const student = await newPage(browser);
  await activate(student, studentLink, 'Örjan Elev', studentEmail);

  await expect(student.getByRole('heading', { name: className })).toBeVisible();
  await expect(student.getByText('Din roll: Elev')).toBeVisible();
  await expect(student.getByRole('heading', { name: 'Bjud in' })).toHaveCount(0);
  await expect(student.getByRole('heading', { name: 'Medlemmar' })).toHaveCount(0);

  await student.getByRole('link', { name: 'Mina klasser' }).click();
  await expect(student.getByRole('heading', { name: 'Mina klasser' })).toBeVisible();
  await expect(student.getByText(className)).toBeVisible();
});

test('en använd inbjudningslänk ger ett begripligt meddelande', async ({ browser }) => {
  const page = await newPage(browser);
  await page.goto(studentLink);

  await expect(page.getByRole('alert')).toHaveText('Länken gäller inte längre. Be den som bjöd in dig om en ny.');
  await expectAccessible(page);
});

test('utloggning, fel lösenord och inloggning', async ({ browser }) => {
  const page = await newPage(browser);
  await page.goto('/');
  await expect(page.getByRole('heading', { name: 'Logga in' })).toBeVisible();
  await expectAccessible(page);

  await page.getByLabel('E-postadress').fill(studentEmail);
  await page.getByLabel('Lösenord', { exact: true }).fill('fel-lösenord-som-är-långt');
  await page.getByRole('button', { name: 'Logga in' }).click();
  await expect(page.getByRole('alert')).toHaveText('Fel e-postadress eller lösenord.');
  await expect(page.getByLabel('E-postadress')).toHaveValue(studentEmail);
  await expectAccessible(page);

  await page.getByRole('button', { name: 'Visa lösenord' }).click();
  await expect(page.getByRole('button', { name: 'Dölj lösenord' })).toBeVisible();
  await page.getByLabel('Lösenord', { exact: true }).fill(password);
  await page.getByRole('button', { name: 'Logga in' }).click();
  await expect(page.getByRole('heading', { name: 'Mina klasser' })).toBeVisible();

  await page.getByRole('button', { name: 'Logga ut' }).click();
  await expect(page.getByRole('heading', { name: 'Logga in' })).toBeVisible();
  await page.goto('/');
  await expect(page.getByRole('heading', { name: 'Logga in' })).toBeVisible();
});

test('läraren ser medlemmarna och återställer elevens lösenord', async ({ browser }) => {
  await teacher.goto(classPath);
  await expect(teacher.getByRole('heading', { name: 'Medlemmar' })).toBeVisible();
  await expectAccessible(teacher);
  // Lärare kan inte återställa lösenord för lärare.
  await expect(teacher.getByRole('button', { name: 'Återställ lösenord för Lena Lärare' })).toHaveCount(0);

  await teacher.getByRole('button', { name: 'Återställ lösenord för Örjan Elev' }).click();
  const resetLink = await sharedLink(teacher);

  const student = await newPage(browser);
  await student.goto(resetLink);
  await expect(student.getByRole('heading', { name: 'Välj nytt lösenord' })).toBeVisible();
  await expectAccessible(student);
  await student.getByLabel('Nytt lösenord').fill('elva-tecken');
  await student.getByRole('button', { name: 'Spara lösenord' }).click();
  await expect(student.getByText('Lösenordet måste vara 12–128 tecken.')).toBeVisible();
  await student.getByLabel('Nytt lösenord').fill('ett-helt-nytt-lösenord');
  await student.getByRole('button', { name: 'Spara lösenord' }).click();
  await expect(student.getByRole('heading', { name: 'Mina klasser' })).toBeVisible();
});

test('inloggad användare går med i en klass, och läraren kan inte återställa en administratörs lösenord', async ({ browser }) => {
  // UC-04 A2: administratören är redan inloggad och går med i klassen som elev.
  await teacher.goto(classPath);
  await teacher.getByRole('button', { name: 'Skapa inbjudningslänk' }).click();
  const link = await sharedLink(teacher);
  const admin = await (await browser.newContext({ storageState: adminState })).newPage();
  await admin.goto(link);
  await expect(admin.getByText('Du är inloggad som Ada Admin.')).toBeVisible();
  await expectAccessible(admin);
  await admin.getByRole('button', { name: 'Gå med i klassen' }).click();
  await expect(admin.getByText('Din roll: Elev')).toBeVisible();

  await teacher.goto(classPath);
  await expect(teacher.getByRole('button', { name: 'Ada Admin, Elev' })).toBeVisible();
  await expect(teacher.getByRole('button', { name: 'Återställ lösenord för Ada Admin' })).toHaveCount(0);
});

test('läraren ändrar elevens roll', async () => {
  await teacher.goto(classPath);
  // Med tangentbordet (WCAG 2.1.1): öppna rollväljaren och välj ny roll.
  await teacher.getByRole('button', { name: 'Örjan Elev, Elev' }).press('Enter');
  await teacher.getByRole('radio', { name: 'Elevrepresentant' }).press('Space');

  await expect(teacher.getByText('Örjan Elev är nu elevrepresentant.')).toBeVisible();
  await expect(teacher.getByRole('button', { name: 'Örjan Elev, Elevrepresentant' })).toBeVisible();
});
