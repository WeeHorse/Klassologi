import { defineConfig } from 'eslint/config';
import tseslint from 'typescript-eslint';

export default defineConfig(
  { ignores: ['dist/', 'playwright-report/', 'test-results/'] },
  tseslint.configs.recommended,
  {
    rules: {
      // Inget innerHTML eller motsvarande: användarinnehåll renderas med lit-html, som escapar text (skydd mot XSS).
      'no-restricted-syntax': [
        'error',
        {
          selector: "MemberExpression[property.name=/^(innerHTML|outerHTML)$/]",
          message: 'Använd lit-html i stället för innerHTML/outerHTML (XSS).',
        },
        {
          selector: "CallExpression[callee.property.name='insertAdjacentHTML']",
          message: 'Använd lit-html i stället för insertAdjacentHTML (XSS).',
        },
        {
          selector: "CallExpression[callee.object.name='document'][callee.property.name=/^(write|writeln)$/]",
          message: 'document.write är inte tillåtet (XSS).',
        },
      ],
      'no-restricted-imports': [
        'error',
        {
          patterns: [
            {
              group: ['lit-html/directives/unsafe-html*', 'lit-html/directives/unsafe-svg*'],
              message: 'unsafeHTML/unsafeSVG kringgår escaping (XSS).',
            },
          ],
        },
      ],
    },
  },
);
