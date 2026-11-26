import { describe, expect, it } from 'vitest';
import { absoluteLink, roleLabel, roles } from './labels';

describe('roleLabel', () => {
  it('uses the words of students and teachers (heuristik 2)', () => {
    expect(roles.map(roleLabel)).toEqual(['Elev', 'Elevrepresentant', 'Lärare']);
  });

  it('handles a missing role', () => {
    expect(roleLabel(null)).toBe('Ingen roll');
  });
});

describe('absoluteLink', () => {
  it('keeps the token in the fragment', () => {
    expect(absoluteLink('/aktivera#abc', 'https://klassologi.se')).toBe('https://klassologi.se/aktivera#abc');
  });
});
