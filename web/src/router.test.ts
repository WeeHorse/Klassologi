import { describe, expect, it } from 'vitest';
import { matchRoute } from './router';

describe('matchRoute', () => {
  it.each([
    ['/', { name: 'home' }],
    ['/logga-in', { name: 'login' }],
    ['/logga-in/', { name: 'login' }],
    ['/aktivera', { name: 'activate' }],
    ['/aterstall', { name: 'reset' }],
    ['/status', { name: 'status' }],
    ['/klasser/01a21831-ad97-7b0c-b53c-c027860f400d', { name: 'class', classId: '01a21831-ad97-7b0c-b53c-c027860f400d' }],
  ])('maps %s', (path, route) => {
    expect(matchRoute(path)).toEqual(route);
  });

  it.each(['/klasser/abc', '/klasser/', '/okand', '/aktivera/extra'])('treats %s as not found', (path) => {
    expect(matchRoute(path)).toEqual({ name: 'notFound' });
  });
});
