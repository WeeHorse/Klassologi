// @vitest-environment jsdom
import { render } from 'lit-html';
import { describe, expect, it } from 'vitest';
import { renderMarkdown, safeHref } from './markdown';

function show(source: string): HTMLElement {
  const container = document.createElement('div');
  render(renderMarkdown(source), container);
  return container;
}

describe('renderMarkdown – stöd', () => {
  it('renders emphasis, strong and line breaks', () => {
    const el = show('**Viktigt:** ny *sal*\nrad två');
    expect(el.querySelector('strong')?.textContent).toBe('Viktigt:');
    expect(el.querySelector('em')?.textContent).toBe('sal');
    expect(el.querySelector('br')).not.toBeNull();
  });

  it('renders lists', () => {
    const el = show('- ett\n- två\n\n1. a\n2. b');
    expect([...el.querySelectorAll('ul li')].map((li) => li.textContent)).toEqual(['ett', 'två']);
    expect(el.querySelectorAll('ol li')).toHaveLength(2);
  });

  it('renders headings at level 4 and 5, below the post title', () => {
    const el = show('# Stor\n## Mindre\n### Minst');
    expect(el.querySelector('h4')?.textContent).toBe('Stor');
    expect([...el.querySelectorAll('h5')].map((h) => h.textContent)).toEqual(['Mindre', 'Minst']);
    expect(el.querySelector('h1, h2, h3')).toBeNull();
  });

  it('renders quotes and code', () => {
    const el = show('> citat\n\n`<b>`\n\n```\nkod <i>\n```');
    expect(el.querySelector('blockquote')?.textContent?.trim()).toBe('citat');
    expect(el.querySelector('p code')?.textContent).toBe('<b>');
    expect(el.querySelector('pre code')?.textContent).toBe('kod <i>');
  });

  it('renders safe links that open in a new tab without access to the opener', () => {
    const a = show('[Schema](https://skolan.se/schema) och https://example.se').querySelectorAll('a');
    expect([...a].map((x) => x.href)).toEqual(['https://skolan.se/schema', 'https://example.se/']);
    expect(a[0].rel).toContain('noopener');
    expect(a[0].target).toBe('_blank');
  });

  it('keeps escaped characters as text', () => {
    const el = show('\\*inte kursiv\\*');
    expect(el.textContent?.trim()).toBe('*inte kursiv*');
    expect(el.querySelector('em')).toBeNull();
  });
});

describe('renderMarkdown – säkerhet (XSS)', () => {
  it.each([
    ['script-tagg', '<script>alert(1)</script>', 'script'],
    ['bild med onerror', '<img src=x onerror="alert(1)">', 'img'],
    ['inbäddad iframe', 'text <iframe src="https://evil.example"></iframe>', 'iframe'],
    ['svg med onload', '<svg onload="alert(1)"></svg>', 'svg'],
  ])('shows raw HTML as text: %s', (_name, source, tag) => {
    const el = show(source);
    expect(el.querySelector(tag)).toBeNull();
    expect(el.textContent).toContain('<');
  });

  it.each(['javascript:alert(1)', 'JaVaScRiPt:alert(1)', 'data:text/html,<script>alert(1)</script>', 'vbscript:x', '/relativ'])(
    'does not create links for %s',
    (href) => {
      const el = show(`[klick](${href})`);
      expect(el.querySelector('a')).toBeNull();
      expect(el.textContent).toContain('klick');
    },
  );

  it('does not show images but turns safe image sources into links', () => {
    const el = show('![bild](https://example.se/spårning.png)');
    expect(el.querySelector('img')).toBeNull();
    expect(el.querySelector('a')?.textContent).toBe('bild');
  });

  it('never creates event handler attributes', () => {
    const el = show('[x](https://example.se "titel" onclick="alert(1)") **<b onmouseover=alert(1)>**');
    const withHandlers = [...el.querySelectorAll('*')].filter((e) => [...e.attributes].some((a) => a.name.startsWith('on')));
    expect(withHandlers).toEqual([]);
  });
});

describe('safeHref', () => {
  it('allows http, https and mailto only', () => {
    expect(safeHref('https://a.se')).toBe('https://a.se/');
    expect(safeHref('mailto:larare@skolan.se')).toBe('mailto:larare@skolan.se');
    expect(safeHref('javascript:alert(1)')).toBeNull();
    expect(safeHref('ftp://a.se')).toBeNull();
  });
});
