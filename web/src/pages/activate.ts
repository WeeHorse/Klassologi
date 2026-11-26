import { html, render, type TemplateResult } from 'lit-html';
import { api, fieldError, problemMessage, type InvitationInfo, type Me, type Problem } from '../api';
import { navigate } from '../router';
import { errorText, inputValue, invalidClass, page, passwordInput, roleLabel, setReturnTo, submitButton } from '../ui';

/** UC-04 Aktivera konto via inbjudan. Token läses från länkens fragment, som inte skickas till servern. */
export async function mountActivate(root: HTMLElement, alive: () => boolean): Promise<void> {
  const token = location.hash.slice(1);
  const state = {
    displayName: '',
    email: '',
    password: '',
    showPassword: false,
    busy: false,
    problem: undefined as Problem | undefined,
    emailTaken: false,
  };

  const show = (content: TemplateResult) => render(page('Aktivera konto', content), root);
  show(html`<p role="status">Kontrollerar inbjudan …</p>`);

  const invitation = token ? await api.inspectInvitation(token) : undefined;
  const me = await api.me();
  if (!alive()) return;

  if (!invitation?.ok) {
    show(html`
      ${errorText(invitation ? problemMessage(invitation.problem) : 'Länken gäller inte längre. Be den som bjöd in dig om en ny.')}
      <p><a href="/logga-in">Till inloggningen</a></p>
    `);
    return;
  }
  const info = invitation.data;

  const done = (classId: string | null) => navigate(classId ? `/klasser/${classId}` : '/', { replace: true });

  const accept = async (account?: { displayName: string; email: string; password: string }) => {
    state.busy = true;
    state.problem = undefined;
    state.emailTaken = false;
    update();
    const result = await api.acceptInvitation(token, account);
    if (!alive()) return;
    state.busy = false;
    if (result.ok) return done(result.data.classId);
    state.problem = result.problem;
    state.emailTaken = result.problem.code === 'email_taken';
    state.password = ''; // Övriga inmatade värden behålls, lösenordet töms (specifikationen, steg 3).
    update();
  };

  const login = () => {
    setReturnTo(`/aktivera#${token}`);
    navigate('/logga-in');
  };

  const submit = (event: Event) => {
    event.preventDefault();
    accept({ displayName: state.displayName.trim(), email: state.email.trim(), password: state.password });
  };

  const update = () => show(html`
    ${invitationSummary(info)}
    ${me.ok ? joinAsExisting(me.data, info, state.busy, () => accept()) : newAccountForm()}
  `);

  const newAccountForm = () => {
    const p = state.problem;
    const generalError = p && p.status !== 400 && !state.emailTaken ? problemMessage(p) : undefined;
    return html`
      <form class="stack" @submit=${submit} novalidate>
        <ion-input
          label="Ditt namn"
          label-placement="stacked"
          fill="outline"
          autocomplete="name"
          required
          maxlength="100"
          helper-text="Så visas du för klassen."
          error-text=${fieldError(p, 'displayName') ?? ''}
          class=${invalidClass(fieldError(p, 'displayName'))}
          .value=${state.displayName}
          @ionInput=${(e: Event) => (state.displayName = inputValue(e))}
        ></ion-input>
        <ion-input
          label="E-postadress"
          label-placement="stacked"
          fill="outline"
          type="email"
          autocomplete="username"
          required
          helper-text="Används när du loggar in."
          error-text=${fieldError(p, 'email') ?? ''}
          class=${invalidClass(fieldError(p, 'email'))}
          .value=${state.email}
          @ionInput=${(e: Event) => (state.email = inputValue(e))}
        ></ion-input>
        ${passwordInput({
          label: 'Välj lösenord',
          value: state.password,
          visible: state.showPassword,
          autocomplete: 'new-password',
          helper: 'Minst 12 tecken. Gärna en mening som du kommer ihåg.',
          error: fieldError(p, 'password'),
          onInput: (v) => (state.password = v),
          onToggle: () => {
            state.showPassword = !state.showPassword;
            update();
          },
        })}
        ${state.emailTaken
          ? html`
              ${errorText(problemMessage(p!))}
              <ion-button fill="outline" @click=${login}>Logga in och gå med</ion-button>
            `
          : errorText(generalError)}
        ${submitButton('Skapa konto', 'Skapar konto …', state.busy)}
      </form>
      <p class="hint">Har du redan ett konto? <a href="/logga-in" @click=${(e: Event) => { e.preventDefault(); login(); }}>Logga in</a> för att gå med.</p>
    `;
  };

  update();
}

function invitationSummary(info: InvitationInfo): TemplateResult {
  return info.isAdministrator
    ? html`<p class="lead">Du är inbjuden som <strong>administratör</strong>.</p>`
    : html`<p class="lead">Du är inbjuden till <strong>${info.className}</strong> som <strong>${roleLabel(info.role)}</strong>.</p>`;
}

function joinAsExisting(me: Me, info: InvitationInfo, busy: boolean, join: () => void): TemplateResult {
  return html`
    <p>Du är inloggad som ${me.displayName}.</p>
    <ion-button expand="block" ?disabled=${busy} @click=${join}>
      ${info.isAdministrator ? 'Bli administratör' : 'Gå med i klassen'}
    </ion-button>
  `;
}
