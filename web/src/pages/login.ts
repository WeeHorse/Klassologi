import { html, render } from 'lit-html';
import { api, problemMessage } from '../api';
import { navigate } from '../router';
import { errorText, inputValue, page, passwordInput, submitButton, takeReturnTo } from '../ui';

/** UC-05 Logga in. */
export async function mountLogin(root: HTMLElement, alive: () => boolean): Promise<void> {
  const state = { email: '', password: '', showPassword: false, busy: false, error: undefined as string | undefined };

  const me = await api.me();
  if (!alive()) return;
  if (me.ok) return navigate(takeReturnTo() ?? '/', { replace: true });

  const submit = async (event: Event) => {
    event.preventDefault();
    state.busy = true;
    state.error = undefined;
    update();
    const result = await api.login(state.email.trim(), state.password);
    if (!alive()) return;
    state.busy = false;
    if (result.ok) return navigate(takeReturnTo() ?? '/', { replace: true });
    state.password = '';
    state.error = problemMessage(result.problem);
    update();
  };

  const update = () =>
    render(
      page(
        'Logga in',
        html`
          <form class="stack" @submit=${submit} novalidate>
            <ion-input
              label="E-postadress"
              label-placement="stacked"
              fill="outline"
              type="email"
              autocomplete="username"
              required
              .value=${state.email}
              @ionInput=${(e: Event) => (state.email = inputValue(e))}
            ></ion-input>
            ${passwordInput({
              label: 'Lösenord',
              value: state.password,
              visible: state.showPassword,
              autocomplete: 'current-password',
              onInput: (v) => (state.password = v),
              onToggle: () => {
                state.showPassword = !state.showPassword;
                update();
              },
            })}
            ${errorText(state.error)}
            ${submitButton('Logga in', 'Loggar in …', state.busy)}
          </form>
          <p class="hint">Har du glömt ditt lösenord? Be din lärare om en länk för att välja ett nytt.</p>
        `,
      ),
      root,
    );

  update();
}
