import { html, nothing, type TemplateResult } from 'lit-html';
import { Lexer, type Token, type Tokens } from 'marked';

/**
 * Markdown till lit-html (steg 4). marked används bara för att dela upp texten i delar; mallarna byggs här av ett
 * begränsat urval. Ingen HTML-sträng skapas, så användarens text kan aldrig bli körbar HTML: allt som inte stöds,
 * inklusive rå HTML, visas som text. Länkar blir klickbara bara för http, https och mailto. Bilder visas inte.
 */

const safeSchemes = new Set(['http:', 'https:', 'mailto:']);

export function safeHref(href: string): string | null {
  try {
    const url = new URL(href);
    return safeSchemes.has(url.protocol) ? url.href : null;
  } catch {
    return null;
  }
}

export function renderMarkdown(source: string): TemplateResult {
  const tokens = new Lexer({ gfm: true, breaks: true }).lex(source);
  return html`${tokens.map(block)}`;
}

function block(token: Token): TemplateResult | typeof nothing {
  const t = token as Tokens.Generic;
  switch (token.type) {
    case 'paragraph':
      return html`<p>${inline(t.tokens)}</p>`;
    case 'heading':
      // Inläggets rubrik är nivå 3, så rubriker i texten blir nivå 4–5.
      return (token as Tokens.Heading).depth <= 1 ? html`<h4>${inline(t.tokens)}</h4>` : html`<h5>${inline(t.tokens)}</h5>`;
    case 'list': {
      const list = token as Tokens.List;
      const items = list.items.map(listItem);
      return list.ordered ? html`<ol start=${list.start === '' ? 1 : list.start}>${items}</ol>` : html`<ul>${items}</ul>`;
    }
    case 'blockquote':
      return html`<blockquote>${(t.tokens ?? []).map(block)}</blockquote>`;
    case 'code':
      return html`<pre><code>${(token as Tokens.Code).text}</code></pre>`;
    case 'hr':
      return html`<hr />`;
    case 'text':
      return html`<p>${inline(t.tokens ?? [token])}</p>`;
    case 'space':
    case 'def':
      return nothing;
    default:
      // T.ex. rå HTML och tabeller: visas som den text användaren skrev.
      return html`<p>${token.raw}</p>`;
  }
}

function listItem(item: Tokens.ListItem): TemplateResult {
  const marker = item.task ? (item.checked ? '☑ ' : '☐ ') : nothing;
  const parts = item.tokens
    .filter((t) => t.type !== 'checkbox')
    .map((t) => (t.type === 'text' ? inline((t as Tokens.Text).tokens ?? [t]) : block(t)));
  return html`<li>${marker}${parts}</li>`;
}

function inline(tokens: Token[] | undefined): unknown[] {
  return (tokens ?? []).map((token) => {
    const t = token as Tokens.Generic;
    switch (token.type) {
      case 'text':
        return t.tokens && t.tokens !== tokens ? inline(t.tokens) : (token as Tokens.Text).text;
      case 'strong':
        return html`<strong>${inline(t.tokens)}</strong>`;
      case 'em':
        return html`<em>${inline(t.tokens)}</em>`;
      case 'del':
        return html`<del>${inline(t.tokens)}</del>`;
      case 'codespan':
        return html`<code>${(token as Tokens.Codespan).text}</code>`;
      case 'br':
        return html`<br />`;
      case 'escape':
        return token.raw.slice(1);
      case 'link':
      case 'image': {
        const link = token as Tokens.Link | Tokens.Image;
        const href = safeHref(link.href);
        const label = token.type === 'link' ? inline(t.tokens) : link.text || link.href;
        return href ? html`<a href=${href} target="_blank" rel="noopener noreferrer nofollow">${label}</a>` : label;
      }
      default:
        // T.ex. rå HTML i löptext: visas som text.
        return token.raw;
    }
  });
}
