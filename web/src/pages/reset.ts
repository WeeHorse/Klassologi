import { html, render } from 'lit-html';
import { api, fieldError, problemMessage, type Problem } from '../api';
import { navigate } from '../router';
import { errorText, page, passwordInput, submitButton } from '../ui';

/** Återställa lösenord med en engångslänk. */
export function mountReset(root: HTMLElement, alive: () => boolean): void {
  const token = location.hash.slice(1);
  const state = { password: '', showPassword: false, busy: false, problem: undefined as Problem | undefined };

  const submit = async (event: Event) => {
    event.preventDefault();
    state.busy = true;
    state.problem = undefined;
    update();
    const result = await api.completePasswordReset(token, state.password);
    if (!alive()) return;
    state.busy = false;
    if (result.ok) return navigate('/', { replace: true });
    state.problem = result.problem;
    update();
  };

  const update = () => {
    const p = state.problem;
    render(
      page(
        'Välj nytt lösenord',
        html`
          <form class="stack" @submit=${submit} novalidate>
            ${passwordInput({
              label: 'Nytt lösenord',
              value: state.password,
              visible: state.showPassword,
              autocomplete: 'new-password',
              helper: 'Minst 12 tecken. Du loggas ut från alla andra enheter.',
              error: fieldError(p, 'newPassword'),
              onInput: (v) => (state.password = v),
              onToggle: () => {
                state.showPassword = !state.showPassword;
                update();
              },
            })}
            ${errorText(p && p.status !== 400 ? problemMessage(p) : undefined)}
            ${submitButton('Spara lösenord', 'Sparar …', state.busy)}
          </form>
        `,
      ),
      root,
    );
  };

  update();
}
