import { describe, expect, it } from 'vitest';
import { fieldError, problemMessage, request } from './api';

const respond = (status: number, body?: unknown) => async () =>
  new Response(body === undefined ? null : JSON.stringify(body), { status });

describe('request', () => {
  it('returns data for a successful response', async () => {
    expect(await request('GET', '/x', undefined, respond(200, { a: 1 }))).toEqual({ ok: true, status: 200, data: { a: 1 } });
  });

  it('handles empty successful responses', async () => {
    expect(await request('POST', '/x', undefined, respond(204))).toEqual({ ok: true, status: 204, data: undefined });
  });

  it('returns the problem details with the status for errors', async () => {
    const result = await request('POST', '/x', {}, respond(401, { title: 'Fel e-postadress eller lösenord.', code: 'invalid_credentials' }));
    expect(result).toEqual({
      ok: false,
      status: 401,
      problem: { title: 'Fel e-postadress eller lösenord.', code: 'invalid_credentials', status: 401 },
    });
  });

  it('turns network failures into status 0', async () => {
    const offline = async () => {
      throw new TypeError('Failed to fetch');
    };
    expect(await request('GET', '/x', undefined, offline)).toEqual({ ok: false, status: 0, problem: { status: 0 } });
  });

  it('sends JSON bodies with the right content type and same-origin credentials', async () => {
    let init: RequestInit | undefined;
    await request('POST', '/x', { a: 1 }, async (_input, i) => {
      init = i;
      return new Response(null, { status: 204 });
    });
    expect(init?.body).toBe('{"a":1}');
    expect(init?.headers).toEqual({ 'Content-Type': 'application/json' });
    expect(init?.credentials).toBe('same-origin');
  });
});

describe('problemMessage', () => {
  it('explains network failures', () => {
    expect(problemMessage({ status: 0 })).toMatch(/Kunde inte nå servern/);
  });

  it('explains rate limiting', () => {
    expect(problemMessage({ status: 429 })).toMatch(/Vänta en minut/);
  });

  it('uses the title from the API when there is one', () => {
    expect(problemMessage({ status: 409, title: 'Klassen måste ha minst en lärare.' })).toBe('Klassen måste ha minst en lärare.');
  });

  it('falls back to a general message', () => {
    expect(problemMessage({ status: 500 })).toBe('Något gick fel. Försök igen.');
  });
});

describe('fieldError', () => {
  it('returns the first error for a field', () => {
    expect(fieldError({ status: 400, errors: { email: ['Ange en giltig e-postadress.', 'x'] } }, 'email')).toBe('Ange en giltig e-postadress.');
  });

  it('returns undefined when the field has no error', () => {
    expect(fieldError({ status: 400, errors: {} }, 'email')).toBeUndefined();
    expect(fieldError(undefined, 'email')).toBeUndefined();
  });
});
