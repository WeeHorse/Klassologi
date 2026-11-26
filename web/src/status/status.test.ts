import { describe, expect, it } from 'vitest';
import { loadStatus, statusMessage } from './status';

const respond = (body: unknown, init?: ResponseInit) => async () => new Response(JSON.stringify(body), init);

describe('loadStatus', () => {
  it('returns ok with the migration count when the API reports ok', async () => {
    expect(await loadStatus(respond({ status: 'ok', migrations: 3 }))).toEqual({ state: 'ok', migrations: 3 });
  });

  it('returns error when the API responds with an error status', async () => {
    expect(await loadStatus(respond({}, { status: 500 }))).toEqual({ state: 'error' });
  });

  it('returns error when the API cannot be reached', async () => {
    const unreachable = async () => {
      throw new TypeError('Failed to fetch');
    };
    expect(await loadStatus(unreachable)).toEqual({ state: 'error' });
  });
});

describe('statusMessage', () => {
  it('uses singular and plural for migrations', () => {
    expect(statusMessage({ state: 'ok', migrations: 1 })).toContain('1 migrering.');
    expect(statusMessage({ state: 'ok', migrations: 2 })).toContain('2 migreringar.');
  });

  it('tells the user what to do when the server cannot be reached', () => {
    expect(statusMessage({ state: 'error' })).toMatch(/ladda sedan om sidan/);
  });
});
