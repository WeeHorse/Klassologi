import type { Role } from './api';

/** Texter och format som visas för användaren. Fria från Ionic, så att de kan enhetstestas. */

const roleLabels: Record<Role, string> = {
  Student: 'Elev',
  StudentRepresentative: 'Elevrepresentant',
  Teacher: 'Lärare',
};

export const roles: Role[] = ['Student', 'StudentRepresentative', 'Teacher'];

export const roleLabel = (role: Role | null | undefined): string => (role ? roleLabels[role] : 'Ingen roll');

export function formatDateTime(iso: string): string {
  return new Date(iso).toLocaleString('sv-SE', { dateStyle: 'medium', timeStyle: 'short' });
}

/** En engångslänk som absolut adress, så att den kan delas. */
export const absoluteLink = (url: string, origin = location.origin): string => origin + url;

