import { html, nothing, render } from 'lit-html';
import { api, problemMessage, type ClassSummary, type Link } from '../api';
import { navigate } from '../router';
import { errorText, inputValue, invalidClass, page, roleLabel, shareLink, submitButton } from '../ui';

/** Startsida: Mina klasser. Administratörer kan skapa klasser (UC-01). */
export async function mountHome(root: HTMLElement, alive: () => boolean): Promise<void> {
  const [me, classes] = await Promise.all([api.me(), api.myClasses()]);
  if (!alive()) return;
  if (!me.ok) return navigate('/logga-in', { replace: true });

  const state = {
    classes: classes.ok ? classes.data : ([] as ClassSummary[]),
    loadError: classes.ok ? undefined : problemMessage(classes.problem),
    newName: '',
    busy: false,
    createError: undefined as string | undefined,
    nameError: undefined as string | undefined,
    created: undefined as { name: string; invitation: Link } | undefined,
  };

  const logout = async () => {
    await api.logout();
    navigate('/logga-in', { replace: true });
  };

  const createClass = async (event: Event) => {
    event.preventDefault();
    state.busy = true;
    state.createError = state.nameError = undefined;
    update();
    const result = await api.createClass(state.newName.trim());
    if (!alive()) return;
    state.busy = false;
    if (result.ok) {
      state.created = { name: result.data.name, invitation: result.data.teacherInvitation };
      state.newName = '';
    } else if (result.status === 400) {
      state.nameError = result.problem.errors?.name?.[0];
    } else {
      state.createError = problemMessage(result.problem);
    }
    update();
  };

  const update = () =>
    render(
      page(
        'Mina klasser',
        html`
          <p class="muted">Inloggad som ${me.data.displayName}${me.data.isAdministrator ? ' (administratör)' : ''}.</p>
          ${errorText(state.loadError)}
          ${state.classes.length
            ? html`
                <ion-list inset aria-label="Mina klasser">
                  ${state.classes.map(
                    (c) => html`
                      <ion-item href=${`/klasser/${c.id}`} detail>
                        <ion-label>${c.name}</ion-label>
                        <ion-note slot="end">${roleLabel(c.myRole)}</ion-note>
                      </ion-item>
                    `,
                  )}
                </ion-list>
              `
            : html`<p>Du är inte med i någon klass än. Be din lärare om en inbjudningslänk.</p>`}
          ${me.data.isAdministrator ? createClassSection() : nothing}
        `,
        html`<ion-button @click=${logout}>Logga ut</ion-button>`,
      ),
      root,
    );

  const createClassSection = () => html`
    <section class="section" aria-labelledby="create-class">
      <h2 id="create-class">Skapa klass</h2>
      <form class="stack" @submit=${createClass} novalidate>
        <ion-input
          label="Klassens namn"
          label-placement="stacked"
          fill="outline"
          required
          maxlength="100"
          helper-text="Till exempel Åk 2 – Systemutveckling."
          error-text=${state.nameError ?? ''}
          class=${invalidClass(state.nameError)}
          .value=${state.newName}
          @ionInput=${(e: Event) => (state.newName = inputValue(e))}
        ></ion-input>
        ${errorText(state.createError)}
        ${submitButton('Skapa klass', 'Skapar …', state.busy)}
      </form>
      ${state.created
        ? html`
            <p role="status">Klassen <strong>${state.created.name}</strong> är skapad. Skicka länken till klassens lärare.</p>
            ${shareLink('Inbjudan för lärare', state.created.invitation.url, state.created.invitation.expiresAt)}
          `
        : nothing}
    </section>
  `;

  update();
}
