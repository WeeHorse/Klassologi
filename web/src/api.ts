/**
 * Egen, handskriven klient mot API:t (beslutat 2026-11-26). Typerna följer API-kontraktet i
 * Specifications/UseCases.md. Glidning mot API:t fångas av Playwright-testerna.
 */

export type Role = 'Student' | 'Teacher' | 'StudentRepresentative';

export type Problem = {
  status: number;
  title?: string;
  code?: string;
  errors?: Record<string, string[]>;
};

export type Result<T> = { ok: true; status: number; data: T } | { ok: false; status: number; problem: Problem };

export type Me = { id: string; displayName: string; email: string | null; isAdministrator: boolean };
export type ClassSummary = { id: string; name: string; myRole: Role | null };
export type Link = { url: string; expiresAt: string };
export type CreatedClass = { id: string; name: string; teacherInvitation: Link };
export type Member = { userId: string; displayName: string; role: Role };
export type InvitationInfo = { className: string | null; role: Role | null; isAdministrator: boolean };
export type Accepted = { classId: string | null };

type Fetch = typeof fetch;

export async function request<T>(method: string, path: string, body?: unknown, fetchFn: Fetch = fetch): Promise<Result<T>> {
  let response: Response;
  try {
    response = await fetchFn(path, {
      method,
      credentials: 'same-origin',
      headers: body === undefined ? {} : { 'Content-Type': 'application/json' },
      body: body === undefined ? undefined : JSON.stringify(body),
    });
  } catch {
    return { ok: false, status: 0, problem: { status: 0 } };
  }

  const text = await response.text();
  const json = text ? safeParse(text) : undefined;
  if (response.ok) return { ok: true, status: response.status, data: json as T };
  const problem = (json && typeof json === 'object' ? json : {}) as Problem;
  return { ok: false, status: response.status, problem: { ...problem, status: response.status } };
}

function safeParse(text: string): unknown {
  try {
    return JSON.parse(text);
  } catch {
    return undefined;
  }
}

/** Ett meddelande på klarspråk för ett misslyckat anrop (heuristik 9). */
export function problemMessage(problem: Problem): string {
  if (problem.status === 0) return 'Kunde inte nå servern. Kontrollera din anslutning och försök igen.';
  if (problem.status === 429) return 'För många försök från din anslutning. Vänta en minut och försök igen.';
  if (problem.status === 403) return problem.title ?? 'Du har inte behörighet att göra det här.';
  if (problem.title && problem.status !== 400) return problem.title;
  return 'Något gick fel. Försök igen.';
}

/** Det första felet för ett fält i ett 400-svar. */
export function fieldError(problem: Problem | undefined, field: string): string | undefined {
  return problem?.errors?.[field]?.[0];
}

export const api = {
  login: (email: string, password: string) => request<void>('POST', '/api/auth/login', { email, password }),
  logout: () => request<void>('POST', '/api/auth/logout'),
  me: () => request<Me>('GET', '/api/auth/me'),
  myClasses: () => request<ClassSummary[]>('GET', '/api/classes'),
  getClass: (id: string) => request<ClassSummary>('GET', `/api/classes/${id}`),
  createClass: (name: string) => request<CreatedClass>('POST', '/api/classes', { name }),
  createInvitation: (classId: string, role: Role) => request<Link>('POST', `/api/classes/${classId}/invitations`, { role }),
  members: (classId: string) => request<Member[]>('GET', `/api/classes/${classId}/members`),
  setRole: (classId: string, userId: string, role: Role) =>
    request<void>('PUT', `/api/classes/${classId}/members/${userId}`, { role }),
  createPasswordReset: (classId: string, userId: string) =>
    request<Link>('POST', `/api/classes/${classId}/members/${userId}/password-reset`),
  inspectInvitation: (token: string) => request<InvitationInfo>('POST', '/api/invitations/inspect', { token }),
  acceptInvitation: (token: string, account?: { displayName: string; email: string; password: string }) =>
    request<Accepted>('POST', '/api/invitations/accept', { token, ...account }),
  completePasswordReset: (token: string, newPassword: string) =>
    request<void>('POST', '/api/password-resets/complete', { token, newPassword }),
};
